import { useState } from 'react'
import Landing from './components/Landing.jsx'
import Login from './components/Login.jsx'
import Builder from './components/Builder.jsx'

export default function App() {
  const [view, setView] = useState('home') // 'home' | 'login' | 'builder'

  if (view === 'login') {
    return <Login onSignIn={() => setView('builder')} onBack={() => setView('home')} />
  }
  if (view === 'builder') {
    return <Builder onHome={() => setView('home')} />
  }
  return <Landing onStart={() => setView('login')} />
}
