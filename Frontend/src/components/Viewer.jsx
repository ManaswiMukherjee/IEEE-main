import { useEffect, useRef, useState } from 'react'
import { createCityScene } from '../lib/cityRenderer.js'

export default function Viewer({ model, onClose }) {
  const mountRef = useRef(null)
  const [error, setError] = useState(null)

  useEffect(() => {
    if (!mountRef.current || !model) return
    let scene
    try {
      scene = createCityScene(mountRef.current, model)
    } catch (e) {
      setError(String(e?.message || e))
    }
    return () => scene?.dispose()
  }, [model])

  useEffect(() => {
    const onKey = (e) => e.key === 'Escape' && onClose()
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [onClose])

  return (
    <div className="viewer">
      <div className="viewer__bar">
        <div className="viewer__title">
          <strong>{model?.city?.name || 'City'}</strong>
          <span>
            {model?.blocks?.length ?? 0} blocks · {model?.windmills?.length ?? 0} windmills · drag to orbit, scroll to zoom
          </span>
        </div>
        <button className="viewer__close" onClick={onClose}>
          Close ✕
        </button>
      </div>
      <div className="viewer__canvas" ref={mountRef} />
      {error && <div className="viewer__error">Renderer error: {error}</div>}
    </div>
  )
}
