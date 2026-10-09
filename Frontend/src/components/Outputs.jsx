import { useState } from 'react'

function CopyButton({ json }) {
  const [copied, setCopied] = useState(false)
  async function copy() {
    try {
      await navigator.clipboard.writeText(json)
      setCopied(true)
      setTimeout(() => setCopied(false), 1500)
    } catch {
      /* clipboard may be unavailable */
    }
  }
  return (
    <button className="schema__copy" onClick={copy}>
      {copied ? 'Copied ✓' : 'Copy JSON'}
    </button>
  )
}

function download(filename, json) {
  const blob = new Blob([json], { type: 'application/json' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = filename
  document.body.appendChild(a)
  a.click()
  a.remove()
  URL.revokeObjectURL(url)
}

export default function Outputs({ requirements, cityModel, onGenerateModel, onView, planBusy, planError }) {
  const [tab, setTab] = useState('requirements')

  const reqJson = requirements ? JSON.stringify(requirements, null, 2) : ''
  const planJson = cityModel ? JSON.stringify(cityModel, null, 2) : ''

  const planSummary = cityModel
    ? `${cityModel.building_styles?.length ?? 0} styles · ` +
      `${cityModel.blocks?.length ?? 0} blocks · ` +
      `${cityModel.green_areas?.length ?? 0} green · ` +
      `${cityModel.water_bodies?.length ?? 0} water · ` +
      `${cityModel.windmills?.length ?? 0} windmills`
    : null

  return (
    <div className="schema">
      <div className="tabs">
        <button
          className={`tab ${tab === 'requirements' ? 'is-active' : ''}`}
          onClick={() => setTab('requirements')}
        >
          Requirements
          <span className="tab__sub">Subsystem 2</span>
        </button>
        <button
          className={`tab ${tab === 'plan' ? 'is-active' : ''}`}
          onClick={() => setTab('plan')}
        >
          City Model
          <span className="tab__sub">Subsystem 3</span>
        </button>
      </div>

      {tab === 'requirements' && (
        <>
          {requirements ? (
            <>
              <div className="schema__head">
                <span className="schema__label">Formal requirements</span>
                <CopyButton json={reqJson} />
              </div>
              <pre className="schema__code">{reqJson}</pre>
            </>
          ) : (
            <p className="schema__hint">
              Send a vision with all parameters filled, and the formal requirements
              schema will appear here.
            </p>
          )}
        </>
      )}

      {tab === 'plan' && (
        <>
          {!cityModel && (
            <div className="plan-cta">
              <p className="schema__hint">
                Generate a full <strong>City Model</strong> — building blocks, roads,
                water bodies, grasslands, trees and windmills — validated against{' '}
                <code>city-model.schema.json</code>, ready to download and feed to the
                3D renderer.
              </p>
              <button
                className="btn-primary"
                onClick={onGenerateModel}
                disabled={!requirements || planBusy}
              >
                {planBusy ? 'Generating…' : 'Generate 3D City Model →'}
              </button>
              {!requirements && (
                <p className="schema__note">Create the requirements first.</p>
              )}
              {planError && <p className="schema__error">{planError}</p>}
            </div>
          )}

          {cityModel && (
            <>
              <div className="schema__head">
                <span className="schema__label">
                  Valid ✓ <span className="schema__summary">{planSummary}</span>
                </span>
                <div className="schema__actions">
                  <button className="schema__copy schema__copy--accent" onClick={onView}>
                    View 3D ▸
                  </button>
                  <CopyButton json={planJson} />
                  <button
                    className="schema__copy"
                    onClick={() => download('city-model.json', planJson)}
                  >
                    Download
                  </button>
                  <button className="schema__copy" onClick={onGenerateModel} disabled={planBusy}>
                    {planBusy ? '…' : 'Regenerate'}
                  </button>
                </div>
              </div>
              <pre className="schema__code">{planJson}</pre>
            </>
          )}
        </>
      )}
    </div>
  )
}
