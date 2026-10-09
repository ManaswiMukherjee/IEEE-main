import { useState } from 'react'

const NAV = ['Home', 'Features', 'How It Works', 'Examples', 'About']

function LeafMark() {
  return (
    <svg width="30" height="30" viewBox="0 0 24 24" fill="none" aria-hidden>
      <path
        d="M20 4C9 4 4 9.5 4 17c0 1 .2 2 .5 3C7 14 12 11 18 10c-5 2.3-8.5 6-10.5 11 8 .5 15-4 15-14 0-1.2-.1-2.3-.3-3H20z"
        fill="#2f7d52"
      />
      <path d="M6 20C8 14 12 11 18 10" stroke="#eaf5ee" strokeWidth="1.4" strokeLinecap="round" />
    </svg>
  )
}

function ArrowRight() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" aria-hidden>
      <path d="M5 12h14M13 6l6 6-6 6" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  )
}

function Chevron() {
  return (
    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" aria-hidden>
      <path d="M6 9l6 6 6-6" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  )
}

const FEATURES = [
  {
    label: ['Clean', 'Energy'],
    icon: <path d="M13 2 4 14h6l-1 8 9-12h-6l1-8z" stroke="currentColor" strokeWidth="1.6" fill="none" strokeLinejoin="round" />,
  },
  {
    label: ['Green', 'Spaces'],
    icon: (
      <>
        <path d="M12 21V11" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" />
        <path d="M12 11c0-4 3-7 7-7 0 4-3 7-7 7z" stroke="currentColor" strokeWidth="1.6" fill="none" strokeLinejoin="round" />
        <path d="M12 14c0-3-2.5-5-5.5-5 0 3 2.5 5 5.5 5z" stroke="currentColor" strokeWidth="1.6" fill="none" strokeLinejoin="round" />
      </>
    ),
  },
  {
    label: ['Smart', 'Infrastructure'],
    icon: (
      <>
        <rect x="4" y="9" width="7" height="12" stroke="currentColor" strokeWidth="1.6" fill="none" rx="1" />
        <rect x="13" y="3" width="7" height="18" stroke="currentColor" strokeWidth="1.6" fill="none" rx="1" />
        <path d="M6.5 12h2M6.5 15h2M15.5 6h2M15.5 9h2M15.5 12h2" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" />
      </>
    ),
  },
  {
    label: ['Low Carbon', 'Footprint'],
    icon: (
      <>
        <path d="M20 8a8 8 0 1 0-1.5 10" stroke="currentColor" strokeWidth="1.6" fill="none" strokeLinecap="round" />
        <path d="M20 4v4h-4" stroke="currentColor" strokeWidth="1.6" fill="none" strokeLinecap="round" strokeLinejoin="round" />
      </>
    ),
  },
]

const PILLS = [
  {
    label: 'City Size',
    icon: (
      <>
        <rect x="4" y="9" width="6" height="11" stroke="currentColor" strokeWidth="1.5" fill="none" rx="1" />
        <rect x="12" y="4" width="6" height="16" stroke="currentColor" strokeWidth="1.5" fill="none" rx="1" />
      </>
    ),
  },
  {
    label: 'Climate',
    icon: <path d="M12 21V11M12 11c0-4 3-7 7-7 0 4-3 7-7 7zM12 13c0-3-2.5-5-5-5 0 3 2.5 5 5 5z" stroke="currentColor" strokeWidth="1.5" fill="none" strokeLinejoin="round" />,
  },
  {
    label: 'Energy Focus',
    icon: <path d="M13 2 4 14h6l-1 8 9-12h-6l1-8z" stroke="currentColor" strokeWidth="1.5" fill="none" strokeLinejoin="round" />,
  },
  {
    label: 'More Parameters',
    icon: (
      <>
        <path d="M4 7h10M18 7h2M4 17h2M10 17h10" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
        <circle cx="16" cy="7" r="2.2" stroke="currentColor" strokeWidth="1.5" fill="none" />
        <circle cx="8" cy="17" r="2.2" stroke="currentColor" strokeWidth="1.5" fill="none" />
      </>
    ),
  },
]

const APPROACH = [
  {
    img: '/approach-1.jpg',
    title: 'Accelerate urban planning workflows',
    body:
      'Gain a deeper understanding of real-world urban contexts with 3D models. Create and iterate on urban development projects using existing data, or design new cities from scratch. CEWNity consumes data for urban planning environments to create procedural models you can use to test your ideas and solve problems.',
    icon: (
      <>
        <path d="M9 3 3 5v16l6-2 6 2 6-2V3l-6 2-6-2z" stroke="currentColor" strokeWidth="1.5" fill="none" strokeLinejoin="round" />
        <path d="M9 3v16M15 5v16" stroke="currentColor" strokeWidth="1.5" />
        <circle cx="12" cy="10" r="1.4" fill="currentColor" />
      </>
    ),
  },
  {
    img: '/approach-2.jpg',
    title: 'Elevate architectural designs',
    body:
      'Create realistic, place-based building models and iterate between different parameters by adjusting floor plans, size, architectural styles, and textures. View structures in context to analyze factors such as zoning requirements, natural resource preservation, viewshed, and more.',
    icon: (
      <>
        <rect x="4" y="9" width="6" height="11" stroke="currentColor" strokeWidth="1.5" fill="none" rx="1" />
        <rect x="12" y="4" width="6" height="16" stroke="currentColor" strokeWidth="1.5" fill="none" rx="1" />
        <path d="M6.5 12h1.5M6.5 15h1.5M14.5 7h1.5M14.5 10h1.5M14.5 13h1.5" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" />
      </>
    ),
  },
  {
    img: '/approach-3.jpg',
    title: 'Create stunning, immersive experiences',
    body:
      'Take your CEWNity models to new heights by leveraging integrations with industry-leading game engines. Connect CEWNity to powerful animation tools to create hyper-realistic 3D scenes, stunning visual effects, and interactive experiences and simulations.',
    icon: (
      <>
        <path d="M12 3 4 7v10l8 4 8-4V7l-8-4z" stroke="currentColor" strokeWidth="1.5" fill="none" strokeLinejoin="round" />
        <path d="M4 7l8 4 8-4M12 11v10" stroke="currentColor" strokeWidth="1.5" strokeLinejoin="round" />
      </>
    ),
  },
]

function Check() {
  return (
    <svg className="plan__check" width="18" height="18" viewBox="0 0 24 24" fill="none" aria-hidden>
      <path d="M5 12.5l4.5 4.5L19 7" stroke="#2f8255" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  )
}

const PLANS = [
  {
    name: 'Free',
    desc: 'Perfect for exploring and getting started with CEWNity.',
    price: '$0',
    cta: 'Get Started',
    popular: false,
    icon: (
      <>
        <path d="M12 21V11" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" />
        <path d="M12 11c0-4 3-7 7-7 0 4-3 7-7 7z" stroke="currentColor" strokeWidth="1.6" fill="none" strokeLinejoin="round" />
        <path d="M12 14c0-3-2.5-5-5.5-5 0 3 2.5 5 5.5 5z" stroke="currentColor" strokeWidth="1.6" fill="none" strokeLinejoin="round" />
      </>
    ),
    features: [
      'Basic city generation (limited)',
      'Access to core parameters',
      'Standard processing speed',
      'Community support',
    ],
  },
  {
    name: 'Pro',
    desc: 'For professionals and teams building smarter, greener cities.',
    price: '$19',
    cta: 'Upgrade to Pro',
    popular: true,
    icon: (
      <>
        <rect x="4" y="9" width="6" height="11" stroke="currentColor" strokeWidth="1.6" fill="none" rx="1" />
        <rect x="12" y="4" width="6" height="16" stroke="currentColor" strokeWidth="1.6" fill="none" rx="1" />
        <path d="M6.5 12h1.5M6.5 15h1.5M14.5 7h1.5M14.5 10h1.5M14.5 13h1.5" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" />
      </>
    ),
    features: [
      'Everything in Free',
      'Advanced parameters & controls',
      'Faster generation speed',
      'Save & manage multiple projects',
      'Priority support',
    ],
  },
  {
    name: 'Max',
    desc: 'For organizations, developers and large-scale urban planning teams.',
    price: '$49',
    cta: 'Upgrade to Max',
    popular: false,
    icon: <path d="M3 7l4 4 5-6 5 6 4-4v11H3V7z" stroke="currentColor" strokeWidth="1.6" fill="none" strokeLinejoin="round" />,
  },
]
PLANS[2].features = [
  'Everything in Pro',
  'Unlimited city generations',
  'Custom parameters & API access',
  'Team collaboration',
  'Dedicated support',
]

const TRUST = [
  {
    title: 'Secure & Reliable',
    sub: 'Your data, our priority',
    icon: <path d="M20 4C9 4 4 9.5 4 17c0 1 .2 2 .5 3C7 14 12 11 18 10c-5 2.3-8.5 6-10.5 11 8 .5 15-4 15-14 0-1.2-.1-2.3-.3-3H20z" fill="#2f8255" />,
  },
  {
    title: 'Flexible Plans',
    sub: 'Scale as you grow',
    icon: <path d="M12 3l7 3v5c0 4.5-3 8-7 10-4-2-7-5.5-7-10V6l7-3z" stroke="#2f8255" strokeWidth="1.6" fill="none" strokeLinejoin="round" />,
  },
  {
    title: 'Sustainable Impact',
    sub: 'Building a greener tomorrow',
    icon: (
      <>
        <circle cx="12" cy="12" r="9" stroke="#2f8255" strokeWidth="1.6" fill="none" />
        <path d="M3 12h18M12 3c3 3 3 15 0 18M12 3c-3 3-3 15 0 18" stroke="#2f8255" strokeWidth="1.4" fill="none" />
      </>
    ),
  },
]

function LeafBadge() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" aria-hidden>
      <path d="M20 4C9 4 4 9.5 4 17c0 1 .2 2 .5 3C7 14 12 11 18 10c-5 2.3-8.5 6-10.5 11 8 .5 15-4 15-14 0-1.2-.1-2.3-.3-3H20z" fill="#2f8255" />
    </svg>
  )
}

export default function Landing({ onStart }) {
  const [prompt, setPrompt] = useState('')
  const submit = () => onStart(prompt.trim() || undefined)

  return (
    <div className="landing">
      <section className="hero-wrap">
        <img className="landing__hero-img" src="/hero.png" alt="A sustainable waterfront city with green towers, parks and wind turbines" />
        <div className="landing__fade" />

        <header className="nav">
          <div className="nav__brand">
            <img src="/logo-transparent.png" alt="CEWNity Logo" className="nav__logo-img" />
          </div>
          <nav className="nav__links">
            {NAV.map((item, i) => (
              <a key={item} href="#" className={i === 0 ? 'is-active' : ''} onClick={(e) => e.preventDefault()}>
                {item}
              </a>
            ))}
          </nav>
          <button className="btn btn--primary nav__cta" onClick={() => onStart()}>
            Get Started <ArrowRight />
          </button>
        </header>

        <main className="hero">
          <div className="hero__content">
            <p className="hero__eyebrow">Smart Planning. Greener Tomorrow.</p>
            <h1 className="hero__title">
              Turn Your Ideas Into
              <br />
              <span className="hero__title-accent">Sustainable Cities</span>
            </h1>
            <p className="hero__sub">
              Describe your vision and set your parameters — our service builds data-driven,
              sustainable cities designed for a cleaner, greener and healthier future.
            </p>

            <div className="hero__cta">
              <button className="btn btn--primary btn--lg" onClick={() => onStart()}>
                Start Building <ArrowRight />
              </button>
              <button
                className="btn btn--ghost btn--lg"
                onClick={() => document.getElementById('how')?.scrollIntoView({ behavior: 'smooth' })}
              >
                See How It Works
              </button>
            </div>

            <ul className="hero__features">
              {FEATURES.map((f, i) => (
                <li key={i} className="feat">
                  <svg className="feat__icon" width="26" height="26" viewBox="0 0 24 24">
                    {f.icon}
                  </svg>
                  <span className="feat__label">
                    {f.label[0]}
                    <br />
                    {f.label[1]}
                  </span>
                </li>
              ))}
            </ul>
          </div>
        </main>
      </section>

      {/* ===== How It Works ===== */}
      <section className="how" id="how">
        <div className="how__content">
          <p className="how__eyebrow">
            How It Works <span className="how__rule" />
          </p>
          <h2 className="how__title">
            From a Simple Prompt
            <br />
            to a <span className="hero__title-accent">Sustainable City</span>
          </h2>
          <p className="how__sub">
            Our AI-powered platform transforms your vision and parameters into a fully planned,
            sustainable city — with smart infrastructure, green spaces and efficient resource
            management.
          </p>

          <div className="promptcard">
            <div className="promptcard__input">
              <svg className="promptcard__spark" width="20" height="20" viewBox="0 0 24 24" fill="none" aria-hidden>
                <path d="M12 3l1.6 4.8L18 9.4l-4.4 1.6L12 16l-1.6-5L6 9.4l4.4-1.6L12 3z" fill="#2f8255" />
                <path d="M19 14l.7 2 2 .7-2 .7-.7 2-.7-2-2-.7 2-.7.7-2z" fill="#7cc79e" />
              </svg>
              <input
                value={prompt}
                onChange={(e) => setPrompt(e.target.value)}
                onKeyDown={(e) => e.key === 'Enter' && submit()}
                placeholder="Describe your vision for a sustainable city…"
              />
              <button className="promptcard__go" onClick={submit} aria-label="Start building">
                <ArrowRight />
              </button>
            </div>
            <div className="promptcard__pills">
              {PILLS.map((p) => (
                <button key={p.label} className="chip-pill" onClick={() => onStart()}>
                  <svg width="18" height="18" viewBox="0 0 24 24">
                    {p.icon}
                  </svg>
                  {p.label}
                  <Chevron />
                </button>
              ))}
            </div>
          </div>
        </div>

        <div className="how__media">
          <img src="/afterhero.png" alt="Isometric 3D model of a planned city with landmarks, rivers and parks" />
        </div>
      </section>

      {/* ===== Our Approach ===== */}
      <section className="approach">
        <div className="approach__head">
          <p className="approach__eyebrow">
            <span className="approach__rule" />
            Our Approach
            <span className="approach__rule" />
          </p>
          <h2 className="approach__title">An innovative approach to urban design</h2>
          <p className="approach__sub">
            Discover how CEWNity's data-driven approach helps optimize your typical workflows in
            urban design and planning, architecture, and 3D immersive experiences.
          </p>
        </div>

        <div className="approach__grid">
          {APPROACH.map((c) => (
            <article key={c.title} className="acard">
              <div className="acard__img">
                <img src={c.img} alt={c.title} />
              </div>
              <span className="acard__icon">
                <svg width="26" height="26" viewBox="0 0 24 24">
                  {c.icon}
                </svg>
              </span>
              <h3 className="acard__title">{c.title}</h3>
              <p className="acard__body">{c.body}</p>
            </article>
          ))}
        </div>

        <div className="approach__divider">
          <span className="approach__dline" />
          <LeafBadge />
          <span className="approach__dline" />
        </div>
      </section>

      {/* ===== Business Model / Pricing ===== */}
      <section className="pricing">
        <div className="approach__head">
          <p className="approach__eyebrow">
            <span className="approach__rule" />
            Business Model
            <span className="approach__rule" />
          </p>
          <h2 className="approach__title">Choose the Plan That Fits Your Vision</h2>
          <p className="approach__sub">
            From individual creators to large organizations, CEWNity offers flexible plans to bring
            your sustainable city ideas to life.
          </p>
        </div>

        <div className="pricing__grid">
          {PLANS.map((p) => (
            <article key={p.name} className={`plan${p.popular ? ' plan--popular' : ''}`}>
              {p.popular && <span className="plan__badge">Most Popular</span>}
              <div className="plan__top">
                <span className="plan__icon">
                  <svg width="26" height="26" viewBox="0 0 24 24">
                    {p.icon}
                  </svg>
                </span>
                <div>
                  <h3 className="plan__name">{p.name}</h3>
                  <p className="plan__desc">{p.desc}</p>
                </div>
              </div>

              <div className="plan__price">
                <strong>{p.price}</strong>
                <span>/ month</span>
              </div>

              <hr className="plan__rule" />

              <ul className="plan__features">
                {p.features.map((f) => (
                  <li key={f}>
                    <Check />
                    {f}
                  </li>
                ))}
              </ul>

              <button
                className={`btn btn--lg plan__cta ${p.popular ? 'btn--primary' : 'btn--ghost'}`}
                onClick={() => onStart()}
              >
                {p.cta} <ArrowRight />
              </button>
            </article>
          ))}
        </div>

        <div className="pricing__trust">
          {TRUST.map((t, i) => (
            <div key={t.title} className={`trust${i > 0 ? ' trust--div' : ''}`}>
              <svg width="26" height="26" viewBox="0 0 24 24" aria-hidden>
                {t.icon}
              </svg>
              <div>
                <strong>{t.title}</strong>
                <span>{t.sub}</span>
              </div>
            </div>
          ))}
        </div>
      </section>
    </div>
  )
}
