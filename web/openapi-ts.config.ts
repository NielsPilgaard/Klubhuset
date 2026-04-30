import { defineConfig } from '@hey-api/openapi-ts'

export default defineConfig({
  input: '../openapi/Skoleplanen.Api.json',
  output: 'src/api/generated',
  plugins: ['@tanstack/react-query'],
})
