import { Helmet } from 'react-helmet-async'

const SITE_URL = 'https://skoleoverblikket.dk'
const SITE_NAME = 'Skoleoverblikket'

interface SeoMetaProps {
  title: string
  description: string
  path: string
  noindex?: boolean
}

export default function SeoMeta({ title, description, path, noindex }: SeoMetaProps) {
  const fullTitle = `${title} — ${SITE_NAME}`
  const url = `${SITE_URL}${path}`

  return (
    <Helmet>
      <title>{fullTitle}</title>
      <meta name="description" content={description} />
      <meta property="og:title" content={fullTitle} />
      <meta property="og:description" content={description} />
      <meta property="og:url" content={url} />
      <meta property="og:site_name" content={SITE_NAME} />
      <link rel="canonical" href={url} />
      {noindex && <meta name="robots" content="noindex,nofollow" />}
    </Helmet>
  )
}
