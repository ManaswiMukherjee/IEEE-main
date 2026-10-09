import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { readFileSync } from 'node:fs'
import 'dotenv/config'
import Anthropic from '@anthropic-ai/sdk'
import Ajv2020 from 'ajv/dist/2020.js'
import { buildSystemPrompt, validateSchema } from './src/lib/schemaContract.js'
import { buildCityModelSystemPrompt, checkCityModel } from './src/lib/cityModelPrompt.js'

// Load + compile the City Model JSON Schema once at startup.
const cityModelSchemaString = readFileSync(
  new URL('./schemas/city-model.schema.json', import.meta.url),
  'utf8'
)
const validateCityModel = new Ajv2020({ allErrors: true, strict: false }).compile(
  JSON.parse(cityModelSchemaString)
)

// --- Server-side interpret endpoint -----------------------------------------
// Runs inside the Vite dev server (Node), so the API key never reaches the
// browser. The frontend POSTs to /api/interpret; the key lives in .env.
function interpretApiPlugin() {
  return {
    name: 'interpret-api',
    configureServer(server) {
      server.middlewares.use('/api/interpret', async (req, res) => {
        if (req.method !== 'POST') {
          res.statusCode = 405
          res.end(JSON.stringify({ error: 'Method not allowed' }))
          return
        }

        const apiKey = process.env.ANTHROPIC_API_KEY
        if (!apiKey) {
          res.statusCode = 503
          res.end(JSON.stringify({ error: 'no_api_key' }))
          return
        }

        try {
          const body = await readJson(req)
          const { vision, params } = body

          const client = new Anthropic({ apiKey })
          const model = process.env.ANTHROPIC_MODEL || 'claude-haiku-4-5'

          const message = await client.messages.create({
            model,
            max_tokens: 500, // hard cost ceiling — the schema is tiny
            system: buildSystemPrompt(),
            messages: [
              {
                role: 'user',
                content:
                  `Mandatory parameters:\n` +
                  `- location: ${params.location}\n` +
                  `- land_area_km2: ${params.land_area_km2}\n` +
                  `- population: ${params.population}\n` +
                  advancedPrefs(params) +
                  `\nUser's vision:\n"${vision}"\n\n` +
                  `Map the advanced preferences into the allowed schema fields where they apply. Return ONLY the JSON schema object.`,
              },
            ],
          })

          const text = message.content
            .filter((b) => b.type === 'text')
            .map((b) => b.text)
            .join('')

          const schema = extractJson(text)
          const errors = validateSchema(schema)
          if (errors.length) {
            res.statusCode = 422
            res.end(JSON.stringify({ error: 'invalid_schema', details: errors }))
            return
          }

          res.setHeader('Content-Type', 'application/json')
          res.end(JSON.stringify({ schema, model }))
        } catch (err) {
          res.statusCode = 500
          res.end(JSON.stringify({ error: 'api_error', message: String(err?.message || err) }))
        }
      })

      // --- Subsystem 3: generate a renderable City MODEL instance ----------
      server.middlewares.use('/api/citymodel', async (req, res) => {
        if (req.method !== 'POST') {
          res.statusCode = 405
          res.end(JSON.stringify({ error: 'Method not allowed' }))
          return
        }

        const apiKey = process.env.ANTHROPIC_API_KEY
        if (!apiKey) {
          res.statusCode = 503
          res.end(JSON.stringify({ error: 'no_api_key' }))
          return
        }

        try {
          const { requirements, params } = await readJson(req)
          const client = new Anthropic({ apiKey })
          // The detailed model is large + structurally demanding, so Subsystem 3
          // defaults to Sonnet (much higher output limit + better large-JSON
          // fidelity than Haiku). Override with ANTHROPIC_PLAN_MODEL.
          const modelName = process.env.ANTHROPIC_PLAN_MODEL || 'claude-sonnet-5-5'
          // Haiku caps output low; keep it safe. Bigger models get room to spare.
          const maxTokens = /haiku/i.test(modelName) ? 8000 : 16000
          const system = buildCityModelSystemPrompt(cityModelSchemaString)

          const baseUser =
            `Formal requirements (JSON):\n${JSON.stringify(requirements, null, 2)}\n\n` +
            (params ? `Original parameters: ${JSON.stringify(params)}\n` : '') +
            advancedPrefs(params) +
            `\nHonor these advanced preferences when shaping the model — e.g. more green_areas for extensive green spaces, cycle/pedestrian roads + transport blocks for transit-first, taller building_styles for high-rise, more/larger water_bodies for advanced water management, more windmills and solar for carbon-negative, flood-aware water and terrain for flood-resilient.\n` +
            `Generate the full renderable city model. Return ONLY the JSON object.`

          const messages = [{ role: 'user', content: baseUser }]
          let cityModel = null
          let lastErrors = []

          // One generation + up to one validation-guided repair.
          for (let attempt = 0; attempt < 2; attempt++) {
            // Stream to avoid HTTP timeouts on large outputs; low effort keeps
            // thinking overhead (and cost/latency) down for structured JSON.
            const stream = client.messages.stream({
              model: modelName,
              max_tokens: maxTokens,
              output_config: { effort: 'low' },
              system,
              messages,
            })
            const message = await stream.finalMessage()
            const text = message.content
              .filter((b) => b.type === 'text')
              .map((b) => b.text)
              .join('')

            let candidate
            try {
              candidate = extractJson(text)
            } catch {
              lastErrors = ['response was not valid JSON']
              messages.push({ role: 'assistant', content: text })
              messages.push({
                role: 'user',
                content: 'That was not valid JSON. Return ONLY the corrected JSON object.',
              })
              continue
            }

            const schemaOk = validateCityModel(candidate)
            const schemaErrors = schemaOk
              ? []
              : (validateCityModel.errors || []).map((e) => `${e.instancePath || '/'} ${e.message}`)
            const geoErrors = checkCityModel(candidate)
            lastErrors = [...schemaErrors, ...geoErrors]

            if (lastErrors.length === 0) {
              cityModel = candidate
              break
            }

            messages.push({ role: 'assistant', content: JSON.stringify(candidate) })
            messages.push({
              role: 'user',
              content:
                `The JSON had these problems:\n- ${lastErrors.slice(0, 30).join('\n- ')}\n\n` +
                `Fix them and return ONLY the corrected JSON object.`,
            })
          }

          if (!cityModel) {
            res.statusCode = 422
            res.end(JSON.stringify({ error: 'invalid_model', details: lastErrors }))
            return
          }

          res.setHeader('Content-Type', 'application/json')
          res.end(JSON.stringify({ cityModel, modelName, valid: true }))
        } catch (err) {
          res.statusCode = 500
          res.end(JSON.stringify({ error: 'api_error', message: String(err?.message || err) }))
        }
      })
    },
  }
}

// Format any set advanced preferences (anything beyond the 3 required params)
// into a prompt block. Returns '' when none are set.
function advancedPrefs(params) {
  if (!params) return ''
  const required = ['location', 'land_area_km2', 'population']
  const entries = Object.entries(params).filter(([k, v]) => !required.includes(k) && v !== '' && v != null)
  if (!entries.length) return ''
  return (
    `Advanced preferences:\n` +
    entries.map(([k, v]) => `- ${k.replace(/_/g, ' ')}: ${v}`).join('\n') +
    `\n`
  )
}

function readJson(req) {
  return new Promise((resolve, reject) => {
    let data = ''
    req.on('data', (chunk) => (data += chunk))
    req.on('end', () => {
      try {
        resolve(JSON.parse(data || '{}'))
      } catch (e) {
        reject(e)
      }
    })
    req.on('error', reject)
  })
}

// Pull the first well-formed JSON object out of the model's text.
function extractJson(text) {
  const start = text.indexOf('{')
  const end = text.lastIndexOf('}')
  if (start === -1 || end === -1) throw new Error('No JSON object in response')
  return JSON.parse(text.slice(start, end + 1))
}

export default defineConfig({
  plugins: [react(), interpretApiPlugin()],
  server: {
    port: 5173,
    host: true,
  },
})
