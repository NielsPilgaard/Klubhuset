import { defineConfig } from '@hey-api/openapi-ts'

export default defineConfig({
  input: '../openapi/Skoleoverblikket.Api.json',
  output: 'src/api/generated',
  plugins: ['@tanstack/react-query'],
})
