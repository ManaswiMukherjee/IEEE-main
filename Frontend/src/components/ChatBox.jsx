import { useState } from 'react'

const SUGGESTIONS = [
  'A highly walkable coastal city with strong public transport, lots of green space, and solar + wind energy.',
  'A compact, car-free city powered by renewables with parks in every neighborhood.',
  'A green tech hub prioritizing geothermal energy, cycling, and public transit.',
]

export default function ChatBox({ messages, onSend, endRef }) {
  const [input, setInput] = useState('')

  function submit(e) {
    e.preventDefault()
    const text = input.trim()
    if (!text) return
    onSend(text)
    setInput('')
  }

  return (
    <div className="chat">
      <div className="chat__messages">
        {messages.map((m, i) => (
          <div key={i} className={`bubble bubble--${m.role}`}>
            {m.role === 'bot' && <span className="bubble__avatar">◳</span>}
            <div className="bubble__text">{m.text}</div>
          </div>
        ))}
        <div ref={endRef} />
      </div>

      {messages.length <= 1 && (
        <div className="chat__suggestions">
          {SUGGESTIONS.map((s, i) => (
            <button key={i} className="chip" onClick={() => onSend(s)}>
              {s}
            </button>
          ))}
        </div>
      )}

      <form className="chat__input" onSubmit={submit}>
        <textarea
          value={input}
          onChange={(e) => setInput(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
              e.preventDefault()
              submit(e)
            }
          }}
          placeholder="Describe your sustainable city…"
          rows={2}
        />
        <button type="submit" disabled={!input.trim()}>
          Send
        </button>
      </form>
    </div>
  )
}
