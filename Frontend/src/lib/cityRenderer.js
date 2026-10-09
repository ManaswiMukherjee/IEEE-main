// ---------------------------------------------------------------------------
// cityRenderer.js — turns a city-model.schema.json instance into a realistic
// Three.js scene.
//
// Realism levers:
//  - PBR sun (DirectionalLight) + soft shadows, hemisphere fill
//  - Sky shader + image-based lighting (PMREM) so glass + water reflect the sky
//  - ACES tone mapping, sRGB, fog for aerial depth
//  - procedural buildings: setback tiers, podiums, canvas window textures,
//    rooftop features — merged per style for performance
//  - instanced trees, ribbon rivers/roads, animated wind turbines
//
// Framework-agnostic: createCityScene(container, model) -> { dispose }
// ---------------------------------------------------------------------------

import * as THREE from 'three'
import { OrbitControls } from 'three/addons/controls/OrbitControls.js'
import { Sky } from 'three/addons/objects/Sky.js'
import { mergeGeometries } from 'three/addons/utils/BufferGeometryUtils.js'

// --- small helpers ---------------------------------------------------------
function mulberry32(seed) {
  let a = seed >>> 0
  return function () {
    a |= 0
    a = (a + 0x6d2b79f5) | 0
    let t = Math.imul(a ^ (a >>> 15), 1 | a)
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296
  }
}
const lerp = (a, b, t) => a + (b - a) * t
const clamp = (v, lo, hi) => Math.max(lo, Math.min(hi, v))

function setColorAttr(geo, hex) {
  const c = new THREE.Color(hex)
  const n = geo.attributes.position.count
  const arr = new Float32Array(n * 3)
  for (let i = 0; i < n; i++) {
    arr[i * 3] = c.r
    arr[i * 3 + 1] = c.g
    arr[i * 3 + 2] = c.b
  }
  geo.setAttribute('color', new THREE.BufferAttribute(arr, 3))
}

function shade(hex, f) {
  const c = new THREE.Color(hex)
  c.multiplyScalar(f)
  return '#' + c.getHexString()
}

// --- window / facade texture ----------------------------------------------
// Builds a repeating patch of windows for a style, plus an emissive version
// (for dusk/night). UVs on the walls repeat this patch up the building.
function makeFacadeTextures(style, rng) {
  const f = style.facade || {}
  const patch = 6 // windows per patch, both axes
  const px = 48 // pixels per window cell
  const size = patch * px
  const wall = (style.wall_palette && style.wall_palette[0]) || '#b8c2cc'
  const spandrel = f.spandrel_color || shade(wall, 0.8)
  const win = f.window_color || '#9ec6e0'
  const frame = f.frame_color || shade(wall, 0.6)
  const ratio = clamp(f.window_ratio ?? 0.5, 0.05, 0.95)
  const pattern = f.window_pattern || 'grid'
  const lit = f.lit_ratio ?? 0.15

  const base = document.createElement('canvas')
  base.width = base.height = size
  const g = base.getContext('2d')
  const emis = document.createElement('canvas')
  emis.width = emis.height = size
  const ge = emis.getContext('2d')
  ge.fillStyle = '#000'
  ge.fillRect(0, 0, size, size)
  g.fillStyle = spandrel
  g.fillRect(0, 0, size, size)

  // window cell dimensions driven by pattern
  for (let r = 0; r < patch; r++) {
    for (let c = 0; c < patch; c++) {
      let wW = px * ratio
      let wH = px * ratio
      if (pattern === 'horizontal_bands' || pattern === 'full_curtain') wW = px * 0.92
      if (pattern === 'vertical_strips' || pattern === 'full_curtain') wH = px * 0.92
      if (pattern === 'sparse') {
        wW = px * 0.4
        wH = px * 0.45
      }
      const ox = c * px + (px - wW) / 2
      const oy = r * px + (px - wH) / 2
      // slight per-window tint variation
      const v = 0.9 + rng() * 0.2
      const wc = new THREE.Color(win).multiplyScalar(v)
      g.fillStyle = frame
      g.fillRect(c * px + 1, r * px + 1, px - 2, px - 2)
      g.fillStyle = '#' + wc.getHexString()
      g.fillRect(ox, oy, wW, wH)
      // emissive: a fraction of windows are "lit"
      if (rng() < lit) {
        ge.fillStyle = '#' + new THREE.Color(win).lerp(new THREE.Color('#fff2cc'), 0.4).getHexString()
        ge.fillRect(ox, oy, wW, wH)
      }
    }
  }

  const map = new THREE.CanvasTexture(base)
  const emap = new THREE.CanvasTexture(emis)
  for (const t of [map, emap]) {
    t.wrapS = t.wrapT = THREE.RepeatWrapping
    t.anisotropy = 4
    t.colorSpace = THREE.SRGBColorSpace
    t.userData.patch = patch
  }
  return { map, emap, patch }
}

function materialProps(material) {
  switch (material) {
    case 'glass_curtain':
      return { roughness: 0.08, metalness: 0.85 }
    case 'steel':
      return { roughness: 0.3, metalness: 0.7 }
    case 'concrete_panel':
      return { roughness: 0.85, metalness: 0.03 }
    case 'brick':
    case 'masonry':
      return { roughness: 0.95, metalness: 0.0 }
    case 'industrial':
      return { roughness: 0.8, metalness: 0.2 }
    default:
      return { roughness: 0.6, metalness: 0.3 }
  }
}

// one wall quad (PlaneGeometry) positioned for a face, with UVs repeating the
// window patch (cols across, floors up).
function wall(width, height, baseY, patch, floorH, orient, cx, cz, x0, x1, z0, z1) {
  const g = new THREE.PlaneGeometry(width, height)
  const cols = Math.max(1, Math.round(width / floorH))
  const floors = Math.max(1, Math.round(height / floorH))
  const uMax = cols / patch
  const vMax = floors / patch
  const uv = g.attributes.uv
  for (let i = 0; i < uv.count; i++) uv.setXY(i, uv.getX(i) * uMax, uv.getY(i) * vMax)
  if (orient === 'S') g.translate(cx, baseY + height / 2, z1)
  else if (orient === 'N') {
    g.rotateY(Math.PI)
    g.translate(cx, baseY + height / 2, z0)
  } else if (orient === 'E') {
    g.rotateY(Math.PI / 2)
    g.translate(x1, baseY + height / 2, cz)
  } else {
    g.rotateY(-Math.PI / 2)
    g.translate(x0, baseY + height / 2, cz)
  }
  return g
}

function roofCap(cx, cz, w, d, y) {
  const g = new THREE.PlaneGeometry(w, d)
  g.rotateX(-Math.PI / 2)
  g.translate(cx, y, cz)
  return g
}

export function createCityScene(container, model) {
  const W = model.city.size[0]
  const D = model.city.size[1]
  const maxDim = Math.max(W, D)
  const rng = mulberry32((model.city.seed || 1) + 1)
  const time = model.environment?.time_of_day || 'day'

  // --- renderer ---
  const renderer = new THREE.WebGLRenderer({ antialias: true, powerPreference: 'high-performance' })
  renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2))
  renderer.setSize(container.clientWidth, container.clientHeight)
  renderer.shadowMap.enabled = true
  renderer.shadowMap.type = THREE.PCFSoftShadowMap
  renderer.toneMapping = THREE.ACESFilmicToneMapping
  renderer.toneMappingExposure = time === 'night' ? 0.6 : 1.05
  renderer.outputColorSpace = THREE.SRGBColorSpace
  container.appendChild(renderer.domElement)

  const scene = new THREE.Scene()
  const camera = new THREE.PerspectiveCamera(50, container.clientWidth / container.clientHeight, 1, maxDim * 12)
  camera.position.set(W * 0.42, maxDim * 0.65, D * 0.75)

  const controls = new OrbitControls(camera, renderer.domElement)
  controls.enableDamping = true
  controls.dampingFactor = 0.07
  controls.target.set(0, maxDim * 0.02, 0)
  controls.maxPolarAngle = Math.PI * 0.49
  controls.minDistance = maxDim * 0.15
  controls.maxDistance = maxDim * 4

  // --- sky + sun direction by time of day ---
  const sky = new Sky()
  sky.scale.setScalar(maxDim * 40)
  scene.add(sky)
  const u = sky.material.uniforms
  const presets = {
    day: { elev: 52, azi: 150, turb: 6, ray: 1.4, sun: 2.6, hemi: 0.5 },
    dawn: { elev: 8, azi: 100, turb: 9, ray: 2.5, sun: 2.2, hemi: 0.35 },
    dusk: { elev: 7, azi: 250, turb: 10, ray: 3, sun: 2.2, hemi: 0.3 },
    night: { elev: -6, azi: 250, turb: 12, ray: 0.6, sun: 0.15, hemi: 0.12 },
  }
  const P = presets[time] || presets.day
  u.turbidity.value = P.turb
  u.rayleigh.value = P.ray
  u.mieCoefficient.value = 0.005
  u.mieDirectionalG.value = 0.8
  const phi = THREE.MathUtils.degToRad(90 - P.elev)
  const theta = THREE.MathUtils.degToRad(P.azi)
  const sunPos = new THREE.Vector3().setFromSphericalCoords(1, phi, theta)
  u.sunPosition.value.copy(sunPos)

  // image-based lighting from the sky (reflections on glass + water)
  const pmrem = new THREE.PMREMGenerator(renderer)
  const envRT = pmrem.fromScene(scene)
  scene.environment = envRT.texture

  const skyCol = new THREE.Color().setHSL(0.6, 0.5, time === 'night' ? 0.05 : 0.5)
  scene.fog = new THREE.Fog(skyCol.getHex(), maxDim * 0.9, maxDim * 3.2)

  // --- lights ---
  const hemi = new THREE.HemisphereLight(0xbfd4ff, 0x6b6b5a, P.hemi)
  scene.add(hemi)
  const sun = new THREE.DirectionalLight(time === 'night' ? 0x8aa0cc : 0xfff4e6, P.sun)
  sun.position.copy(sunPos).multiplyScalar(maxDim * 2)
  sun.castShadow = true
  sun.shadow.mapSize.set(4096, 4096)
  const s = sun.shadow.camera
  s.left = -W * 0.75
  s.right = W * 0.75
  s.top = D * 0.75
  s.bottom = -D * 0.75
  s.near = maxDim * 0.5
  s.far = maxDim * 6
  sun.shadow.bias = -0.0004
  sun.shadow.normalBias = 1.5
  scene.add(sun)
  scene.add(sun.target)

  // --- root group: build in model coords (0..W, 0..D), then centre ---
  const root = new THREE.Group()
  root.position.set(-W / 2, 0, -D / 2)
  scene.add(root)

  const disposables = []
  const track = (obj) => {
    disposables.push(obj)
    return obj
  }

  // ground
  const groundGeo = new THREE.PlaneGeometry(W * 1.6, D * 1.6)
  groundGeo.rotateX(-Math.PI / 2)
  groundGeo.translate(W / 2, -0.1, D / 2)
  const groundMat = new THREE.MeshStandardMaterial({
    color: model.terrain?.ground_color || '#6f7d55',
    roughness: 1,
    metalness: 0,
  })
  const ground = new THREE.Mesh(groundGeo, groundMat)
  ground.receiveShadow = true
  root.add(ground)
  track(groundGeo)
  track(groundMat)

  // --- green areas + trees ---
  const greenGeos = []
  const treeMatrices = []
  const treeColors = []
  for (const area of model.green_areas || []) {
    const [x, y, w, d] = area.bounds
    const g = new THREE.PlaneGeometry(w, d)
    g.rotateX(-Math.PI / 2)
    g.translate(x + w / 2, 0.05, y + d / 2)
    setColorAttr(g, area.color || (area.kind === 'forest' ? '#3f6b3a' : '#6b9a52'))
    greenGeos.push(g)

    const density = area.tree_density ?? 0.3
    const hRange = area.tree_height_range || [6, 14]
    const spacing = lerp(26, 9, clamp(density, 0, 1))
    const cols = Math.floor(w / spacing)
    const rows = Math.floor(d / spacing)
    for (let r = 0; r < rows; r++) {
      for (let c = 0; c < cols; c++) {
        if (rng() > density * 1.1) continue
        const tx = x + (c + 0.5) * (w / cols) + (rng() - 0.5) * spacing * 0.5
        const tz = y + (r + 0.5) * (d / rows) + (rng() - 0.5) * spacing * 0.5
        const th = lerp(hRange[0], hRange[1], rng())
        const m = new THREE.Matrix4()
        m.compose(
          new THREE.Vector3(tx, 0, tz),
          new THREE.Quaternion().setFromAxisAngle(new THREE.Vector3(0, 1, 0), rng() * Math.PI),
          new THREE.Vector3(th / 10, th / 10, th / 10)
        )
        treeMatrices.push(m)
        treeColors.push(0.25 + rng() * 0.25)
      }
    }
  }
  if (greenGeos.length) {
    const merged = mergeGeometries(greenGeos)
    const mat = new THREE.MeshStandardMaterial({ vertexColors: true, roughness: 1, metalness: 0 })
    const mesh = new THREE.Mesh(merged, mat)
    mesh.receiveShadow = true
    root.add(mesh)
    track(merged)
    track(mat)
    greenGeos.forEach((g) => g.dispose())
  }

  // trees (instanced): trunk + foliage merged, vertex-coloured.
  // NOTE: both parts must match indexed-ness for mergeGeometries — Icosahedron
  // is non-indexed, so convert the (indexed) cylinder to non-indexed too.
  if (treeMatrices.length) {
    const trunkRaw = new THREE.CylinderGeometry(0.6, 0.9, 4, 6)
    trunkRaw.translate(0, 2, 0)
    const trunk = trunkRaw.toNonIndexed()
    trunkRaw.dispose()
    setColorAttr(trunk, '#6b4a2f')
    const foliage = new THREE.IcosahedronGeometry(3.2, 0)
    foliage.translate(0, 6.5, 0)
    foliage.scale(1, 1.25, 1)
    setColorAttr(foliage, '#4a7a3a')
    const treeGeo = mergeGeometries([trunk, foliage])
    if (treeGeo) {
      const treeMat = new THREE.MeshStandardMaterial({ vertexColors: true, roughness: 0.9, metalness: 0 })
      const inst = new THREE.InstancedMesh(treeGeo, treeMat, treeMatrices.length)
      inst.castShadow = true
      inst.receiveShadow = true
      const tint = new THREE.Color()
      treeMatrices.forEach((m, i) => {
        inst.setMatrixAt(i, m)
        tint.setHSL(0.27, 0.45, treeColors[i])
        inst.setColorAt(i, tint)
      })
      inst.instanceMatrix.needsUpdate = true
      root.add(inst)
      track(treeGeo)
      track(treeMat)
    }
    trunk.dispose()
    foliage.dispose()
  }

  // --- water + roads (ribbons / polygons) ---
  function ribbon(points, width, y) {
    const pos = []
    const idx = []
    const n = points.length
    for (let i = 0; i < n; i++) {
      const p = points[i]
      const prev = points[Math.max(0, i - 1)]
      const next = points[Math.min(n - 1, i + 1)]
      const dir = new THREE.Vector2(next[0] - prev[0], next[1] - prev[1])
      if (dir.lengthSq() === 0) dir.set(1, 0)
      dir.normalize()
      const nx = -dir.y
      const ny = dir.x
      pos.push(p[0] + nx * width / 2, y, p[1] + ny * width / 2)
      pos.push(p[0] - nx * width / 2, y, p[1] - ny * width / 2)
    }
    for (let i = 0; i < n - 1; i++) {
      const a = i * 2
      idx.push(a, a + 1, a + 2, a + 1, a + 3, a + 2)
    }
    const g = new THREE.BufferGeometry()
    g.setAttribute('position', new THREE.Float32BufferAttribute(pos, 3))
    g.setIndex(idx)
    g.computeVertexNormals()
    return g
  }
  function polygonGeo(points, y) {
    const shape = new THREE.Shape(points.map((p) => new THREE.Vector2(p[0], p[1])))
    const g = new THREE.ShapeGeometry(shape)
    g.rotateX(-Math.PI / 2)
    g.translate(0, y, 0)
    // ShapeGeometry lies in XY then we rotated; its Y became -Z, so fix mapping:
    return g
  }

  const waterGeos = []
  for (const wb of model.water_bodies || []) {
    let g = null
    if (wb.shape === 'strip' && wb.path) g = ribbon(wb.path, wb.width || 30, 0.3)
    else if (wb.shape === 'area' && wb.polygon) {
      // build flat polygon directly in XZ
      const shape = new THREE.Shape(wb.polygon.map((p) => new THREE.Vector2(p[0], p[1])))
      const flat = new THREE.ShapeGeometry(shape)
      const p = flat.attributes.position
      const np = []
      for (let i = 0; i < p.count; i++) np.push(p.getX(i), 0.3, p.getY(i))
      g = new THREE.BufferGeometry()
      g.setAttribute('position', new THREE.Float32BufferAttribute(np, 3))
      g.setIndex(flat.index)
      g.computeVertexNormals()
      flat.dispose()
    }
    if (g) waterGeos.push(g)
  }
  if (waterGeos.length) {
    const merged = mergeGeometries(waterGeos)
    const mat = new THREE.MeshStandardMaterial({
      color: model.environment?.water_color || '#2e6fa8',
      roughness: 0.06,
      metalness: 0.5,
      transparent: true,
      opacity: 0.88,
    })
    const mesh = new THREE.Mesh(merged, mat)
    mesh.receiveShadow = true
    root.add(mesh)
    track(merged)
    track(mat)
    waterGeos.forEach((g) => g.dispose())
  }

  const roadGeos = []
  for (const r of model.roads || []) {
    if (r.path && r.path.length >= 2) roadGeos.push(ribbon(r.path, r.width || 12, 0.15))
  }
  if (roadGeos.length) {
    const merged = mergeGeometries(roadGeos)
    const mat = new THREE.MeshStandardMaterial({ color: '#2b2e33', roughness: 0.9, metalness: 0 })
    const mesh = new THREE.Mesh(merged, mat)
    mesh.receiveShadow = true
    root.add(mesh)
    track(merged)
    track(mat)
    roadGeos.forEach((g) => g.dispose())
  }

  // --- buildings ---
  const styleMap = {}
  for (const st of model.building_styles || []) styleMap[st.id] = st
  const wallsByStyle = {} // styleId -> geometry[]
  const roofGeos = []
  const detailGeos = []

  function addRoofFeatures(cx, cz, footprint, topY, style) {
    const feats = style.rooftop_features || []
    const pick = feats[Math.floor(rng() * feats.length)]
    const add = (g, color) => {
      setColorAttr(g, color)
      detailGeos.push(g)
    }
    if (pick === 'spire' || (topY > 150 && feats.includes('spire'))) {
      const g = new THREE.ConeGeometry(footprint * 0.12, topY * 0.14, 6)
      g.translate(cx, topY + topY * 0.07, cz)
      add(g, '#9aa3ab')
    } else if (pick === 'antenna') {
      const g = new THREE.CylinderGeometry(0.6, 0.9, topY * 0.1, 5)
      g.translate(cx, topY + topY * 0.05, cz)
      add(g, '#b0b0b0')
    } else if (pick === 'solar_panels') {
      const g = new THREE.BoxGeometry(footprint * 0.7, 0.6, footprint * 0.5)
      g.translate(cx, topY + 1.2, cz)
      add(g, '#20324a')
    } else if (pick === 'water_tank') {
      const g = new THREE.CylinderGeometry(footprint * 0.14, footprint * 0.14, footprint * 0.22, 8)
      g.translate(cx, topY + footprint * 0.11, cz)
      add(g, '#8a7f6a')
    } else {
      // hvac units
      for (let i = 0; i < 2; i++) {
        const bw = footprint * (0.12 + rng() * 0.12)
        const g = new THREE.BoxGeometry(bw, 2 + rng() * 2, bw)
        g.translate(cx + (rng() - 0.5) * footprint * 0.4, topY + 1.5, cz + (rng() - 0.5) * footprint * 0.4)
        add(g, '#9a9a9a')
      }
    }
  }

  function addBuilding(cx, cz, w, d, h, style) {
    const floorH = style.floor_height || 3.5
    const patch = 6
    const tiers = style.setback?.tiers || 1
    const ratio = style.setback?.ratio || 0.82
    const roofColor = shade((style.wall_palette && style.wall_palette[0]) || '#9aa3ab', 0.55)
    if (!wallsByStyle[style.id]) wallsByStyle[style.id] = []
    const arr = wallsByStyle[style.id]

    // podium
    let baseY = 0
    if (style.podium && h > style.podium.height * 1.5) {
      const ph = Math.min(style.podium.height, h * 0.4)
      const pw = w * 1.18
      const pd = d * 1.18
      const x0 = cx - pw / 2, x1 = cx + pw / 2, z0 = cz - pd / 2, z1 = cz + pd / 2
      arr.push(wall(pw, ph, 0, patch, floorH, 'S', cx, cz, x0, x1, z0, z1))
      arr.push(wall(pw, ph, 0, patch, floorH, 'N', cx, cz, x0, x1, z0, z1))
      arr.push(wall(pd, ph, 0, patch, floorH, 'E', cx, cz, x0, x1, z0, z1))
      arr.push(wall(pd, ph, 0, patch, floorH, 'W', cx, cz, x0, x1, z0, z1))
      const rg = roofCap(cx, cz, pw, pd, ph)
      setColorAttr(rg, roofColor)
      roofGeos.push(rg)
      baseY = ph
    }

    const tierH = (h - baseY) / tiers
    let tw = w, td = d
    let topY = baseY
    for (let t = 0; t < tiers; t++) {
      const x0 = cx - tw / 2, x1 = cx + tw / 2, z0 = cz - td / 2, z1 = cz + td / 2
      arr.push(wall(tw, tierH, baseY, patch, floorH, 'S', cx, cz, x0, x1, z0, z1))
      arr.push(wall(tw, tierH, baseY, patch, floorH, 'N', cx, cz, x0, x1, z0, z1))
      arr.push(wall(td, tierH, baseY, patch, floorH, 'E', cx, cz, x0, x1, z0, z1))
      arr.push(wall(td, tierH, baseY, patch, floorH, 'W', cx, cz, x0, x1, z0, z1))
      baseY += tierH
      topY = baseY
      const rg = roofCap(cx, cz, tw, td, baseY)
      setColorAttr(rg, roofColor)
      roofGeos.push(rg)
      tw *= ratio
      td *= ratio
    }
    if (h > 40 && (style.rooftop_features || []).length) addRoofFeatures(cx, cz, Math.max(tw, td) / ratio, topY, style)
  }

  for (const block of model.blocks || []) {
    const style = styleMap[block.style]
    if (!style) continue
    const [bx, by, bw, bd] = block.bounds
    const fr = style.footprint_range || [16, 36]
    const gap = 7
    const cell = (fr[0] + fr[1]) / 2 + gap
    const cols = Math.max(1, Math.floor(bw / cell))
    const rows = Math.max(1, Math.floor(bd / cell))
    const cw = bw / cols
    const cd = bd / rows
    const coverage = style.coverage ?? 0.6
    const [hmin, hmax] = style.height_range
    const bias = block.height_bias || 1
    for (let r = 0; r < rows; r++) {
      for (let c = 0; c < cols; c++) {
        if (rng() > coverage) continue
        const cx = bx + (c + 0.5) * cw
        const cz = by + (r + 0.5) * cd
        const fw = clamp(lerp(fr[0], fr[1], rng()), 4, cw - gap)
        const fd = clamp(lerp(fr[0], fr[1], rng()), 4, cd - gap)
        const h = Math.max(4, lerp(hmin, hmax, Math.pow(rng(), 1.4)) * bias)
        addBuilding(cx, cz, fw, fd, h, style)
      }
    }
  }

  // landmarks -> explicit towers
  for (const lm of model.landmarks || []) {
    const style = styleMap[lm.style] || model.building_styles?.[0] || {
      floor_height: 4,
      facade: { material: 'glass_curtain', window_pattern: 'full_curtain', window_ratio: 0.85 },
      wall_palette: ['#aebfce'],
      setback: { tiers: 3, ratio: 0.8 },
    }
    const [fw, fd] = lm.footprint
    addBuilding(lm.position[0], lm.position[1], fw, fd, lm.height, style)
    if (lm.spire) {
      const g = new THREE.ConeGeometry(Math.min(fw, fd) * 0.18, lm.height * 0.18, 6)
      g.translate(lm.position[0], lm.height + lm.height * 0.09, lm.position[1])
      setColorAttr(g, '#c0c8d0')
      detailGeos.push(g)
    }
  }

  // merge walls per style
  for (const [sid, geos] of Object.entries(wallsByStyle)) {
    if (!geos.length) continue
    const style = styleMap[sid]
    const merged = mergeGeometries(geos)
    const { map, emap } = makeFacadeTextures(style, rng)
    const mp = materialProps(style.facade?.material)
    const emissiveI = time === 'night' ? 1.0 : time === 'dusk' || time === 'dawn' ? 0.5 : 0.08
    const mat = new THREE.MeshStandardMaterial({
      map,
      emissiveMap: emap,
      emissive: 0xffffff,
      emissiveIntensity: emissiveI,
      roughness: mp.roughness,
      metalness: mp.metalness,
      envMapIntensity: 1.1,
    })
    const mesh = new THREE.Mesh(merged, mat)
    mesh.castShadow = true
    mesh.receiveShadow = true
    root.add(mesh)
    track(merged)
    track(mat)
    track(map)
    track(emap)
    geos.forEach((g) => g.dispose())
  }
  if (roofGeos.length) {
    const merged = mergeGeometries(roofGeos)
    const mat = new THREE.MeshStandardMaterial({ vertexColors: true, roughness: 0.85, metalness: 0.1 })
    const mesh = new THREE.Mesh(merged, mat)
    mesh.castShadow = true
    mesh.receiveShadow = true
    root.add(mesh)
    track(merged)
    track(mat)
    roofGeos.forEach((g) => g.dispose())
  }
  if (detailGeos.length) {
    const merged = mergeGeometries(detailGeos)
    const mat = new THREE.MeshStandardMaterial({ vertexColors: true, roughness: 0.6, metalness: 0.4 })
    const mesh = new THREE.Mesh(merged, mat)
    mesh.castShadow = true
    root.add(mesh)
    track(merged)
    track(mat)
    detailGeos.forEach((g) => g.dispose())
  }

  // --- windmills (animated) ---
  const rotors = []
  for (const wm of model.windmills || []) {
    const grp = new THREE.Group()
    grp.position.set(wm.position[0], 0, wm.position[1])
    const hub = wm.hub_height
    const towerMat = new THREE.MeshStandardMaterial({
      color: wm.tower_color || '#eef1f4',
      roughness: 0.4,
      metalness: 0.2,
    })
    const towerGeo = new THREE.CylinderGeometry(hub * 0.012, hub * 0.03, hub, 12)
    towerGeo.translate(0, hub / 2, 0)
    const tower = new THREE.Mesh(towerGeo, towerMat)
    tower.castShadow = true
    grp.add(tower)

    // Stationary nacelle atop the tower
    const nacelleGeo = new THREE.BoxGeometry(hub * 0.08, hub * 0.06, hub * 0.16)
    const nacelle = new THREE.Mesh(nacelleGeo, towerMat)
    nacelle.position.set(0, hub, 0)
    nacelle.castShadow = true
    grp.add(nacelle)

    // Spinning rotor (hub + blades)
    const rotor = new THREE.Group()
    rotor.position.set(0, hub, hub * 0.08 + wm.rotor_radius * 0.02)
    const hubGeo = new THREE.SphereGeometry(hub * 0.035, 12, 12)
    const hubMesh = new THREE.Mesh(hubGeo, towerMat)
    hubMesh.castShadow = true
    rotor.add(hubMesh)

    const blades = wm.blades || 3
    const bladeGeo = new THREE.BoxGeometry(wm.rotor_radius * 0.9, wm.rotor_radius * 0.06, 0.6)
    bladeGeo.translate(wm.rotor_radius * 0.45, 0, 0)
    for (let b = 0; b < blades; b++) {
      const blade = new THREE.Mesh(bladeGeo, towerMat)
      blade.rotation.z = (b / blades) * Math.PI * 2
      blade.castShadow = true
      rotor.add(blade)
    }
    rotor.userData.speed = 1.2 + rng() * 0.8
    grp.add(rotor)
    rotors.push(rotor)
    root.add(grp)
    track(towerGeo)
    track(towerMat)
    track(nacelleGeo)
    track(hubGeo)
    track(bladeGeo)
  }

  // --- animate ---
  let raf = 0
  const clock = new THREE.Clock()
  function animate() {
    raf = requestAnimationFrame(animate)
    const dt = clock.getDelta()
    for (const r of rotors) r.rotation.z += r.userData.speed * dt
    controls.update()
    renderer.render(scene, camera)
  }
  animate()

  // --- resize ---
  const onResize = () => {
    const w = container.clientWidth
    const h = container.clientHeight
    if (!w || !h) return
    camera.aspect = w / h
    camera.updateProjectionMatrix()
    renderer.setSize(w, h)
  }
  const ro = new ResizeObserver(onResize)
  ro.observe(container)

  return {
    dispose() {
      cancelAnimationFrame(raf)
      ro.disconnect()
      controls.dispose()
      disposables.forEach((o) => o.dispose && o.dispose())
      envRT.texture.dispose()
      pmrem.dispose()
      renderer.dispose()
      if (renderer.domElement.parentNode === container) container.removeChild(renderer.domElement)
    },
  }
}
