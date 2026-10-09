import { useState } from 'react'

const DEMO = { email: 'demo@cewnity.app', password: 'cewnity123' }

function ArrowRight() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" aria-hidden>
      <path d="M5 12h14M13 6l6 6-6 6" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  )
}

export default function Login({ onSignIn, onBack }) {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')

  function handleSubmit(e) {
    e.preventDefault()
    if (email.trim() === DEMO.email && password === DEMO.password) {
      setError('')
      onSignIn()
    } else {
      setError('Those credentials don’t match. Use the demo credentials below.')
    }
  }

  function useDemo() {
    setEmail(DEMO.email)
    setPassword(DEMO.password)
    setError('')
  }

  return (
    <div className="login">
      <button className="login__back" onClick={onBack}>
        ← Back to home
      </button>

      <form className="login__card" onSubmit={handleSubmit}>
        <div className="login__brand">
          <img src="/logo-transparent.png" alt="CEWNity Logo" className="login__logo-img" />
        </div>

        <h1 className="login__title">Welcome back</h1>
        <p className="login__sub">Sign in to continue to your sustainable city planner.</p>

        <div className="login__demo">
          <div className="login__demo-head">
            <span>Demo credentials</span>
            <button type="button" className="login__demo-fill" onClick={useDemo}>
              Autofill
            </button>
          </div>
          <code>{DEMO.email}</code>
          <code>{DEMO.password}</code>
        </div>

        <label className="login__field">
          <span>Email</span>
          <input
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="you@example.com"
            autoComplete="username"
          />
        </label>

        <label className="login__field">
          <span>Password</span>
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            placeholder="••••••••"
            autoComplete="current-password"
          />
        </label>

        {error && <p className="login__error">{error}</p>}

        <button type="submit" className="btn btn--primary btn--lg login__submit">
          Sign in <ArrowRight />
        </button>

        <p className="login__foot">
          New here? <a href="#" onClick={(e) => e.preventDefault()}>Create an account</a>
        </p>
      </form>
    </div>
  )
}
