import { useEffect, useRef, useState } from 'react'
import L from 'leaflet'
import 'leaflet/dist/leaflet.css'
import markerIcon from 'leaflet/dist/images/marker-icon.png'
import markerIcon2x from 'leaflet/dist/images/marker-icon-2x.png'
import markerShadow from 'leaflet/dist/images/marker-shadow.png'

// Fix Leaflet's default marker icon paths under a bundler (Vite).
L.Icon.Default.mergeOptions({
  iconUrl: markerIcon,
  iconRetinaUrl: markerIcon2x,
  shadowUrl: markerShadow,
})

const NOMINATIM = 'https://nominatim.openstreetmap.org'

export default function LocationPicker({ initial, onSelect, onClose }) {
  const mapRef = useRef(null)
  const mapObj = useRef(null)
  const markerRef = useRef(null)
  const [query, setQuery] = useState(initial || '')
  const [results, setResults] = useState([])
  const [addr, setAddr] = useState(initial || '')
  const [coords, setCoords] = useState(null)
  const [searching, setSearching] = useState(false)

  // init map
  useEffect(() => {
    if (!mapRef.current || mapObj.current) return
    const map = L.map(mapRef.current, { zoomControl: true }).setView([20, 10], 2)
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '© OpenStreetMap contributors',
    }).addTo(map)
    mapObj.current = map

    map.on('click', (e) => {
      setPin(e.latlng.lat, e.latlng.lng)
      reverseGeocode(e.latlng.lat, e.latlng.lng)
    })

    // Leaflet needs a size recalc once the modal has laid out.
    setTimeout(() => map.invalidateSize(), 60)

    return () => {
      map.remove()
      mapObj.current = null
    }
  }, [])

  function setPin(lat, lng, zoom) {
    const map = mapObj.current
    if (!map) return
    const ll = [lat, lng]
    if (markerRef.current) markerRef.current.setLatLng(ll)
    else markerRef.current = L.marker(ll).addTo(map)
    map.setView(ll, zoom || Math.max(map.getZoom(), 12))
    setCoords({ lat, lng })
  }

  async function reverseGeocode(lat, lng) {
    try {
      const r = await fetch(`${NOMINATIM}/reverse?format=jsonv2&lat=${lat}&lon=${lng}`)
      const j = await r.json()
      setAddr(j.display_name || `${lat.toFixed(5)}, ${lng.toFixed(5)}`)
    } catch {
      setAddr(`${lat.toFixed(5)}, ${lng.toFixed(5)}`)
    }
  }

  async function runSearch(e) {
    e?.preventDefault?.()
    const q = query.trim()
    if (q.length < 3) return
    setSearching(true)
    try {
      const r = await fetch(`${NOMINATIM}/search?format=jsonv2&q=${encodeURIComponent(q)}&limit=6`)
      setResults(await r.json())
    } catch {
      setResults([])
    } finally {
      setSearching(false)
    }
  }

  function pickResult(res) {
    const lat = parseFloat(res.lat)
    const lng = parseFloat(res.lon)
    setAddr(res.display_name)
    setResults([])
    setQuery(res.display_name)
    setPin(lat, lng)
  }

  return (
    <div className="locpick" onClick={onClose}>
      <div className="locpick__panel" onClick={(e) => e.stopPropagation()}>
        <div className="locpick__head">
          <strong>Select location</strong>
          <button className="locpick__close" onClick={onClose} aria-label="Close">
            ✕
          </button>
        </div>

        <form className="locpick__searchrow" onSubmit={runSearch}>
          <input
            className="locpick__search"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="Search for a place, then press Enter…"
          />
          <button type="submit" className="btn btn--primary locpick__searchbtn" disabled={searching}>
            {searching ? '…' : 'Search'}
          </button>
        </form>

        {results.length > 0 && (
          <ul className="locpick__results">
            {results.map((r) => (
              <li key={r.place_id}>
                <button type="button" onClick={() => pickResult(r)}>
                  {r.display_name}
                </button>
              </li>
            ))}
          </ul>
        )}

        <div ref={mapRef} className="locpick__map" />

        <div className="locpick__foot">
          <span className="locpick__addr">{addr || 'Search or click the map to choose a location.'}</span>
          <button className="btn btn--primary" onClick={() => addr.trim() && onSelect(addr.trim(), coords)} disabled={!addr.trim()}>
            Use this location
          </button>
        </div>
      </div>
    </div>
  )
}
