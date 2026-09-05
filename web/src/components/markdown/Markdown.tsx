import ReactMarkdown from 'react-markdown'
import remarkBreaks from 'remark-breaks'
import { MD_ALLOWED_TAGS } from './allowed'

// Single newlines from MarkdownTextarea (e.g. a plain multi-line note without
// bullets) must render as line breaks, matching what staff typed. Plain
// CommonMark collapses a lone "\n" to a space.
const REMARK_PLUGINS = [remarkBreaks]

// CommonMark treats any run of 2+ newlines as a single paragraph break —
// typing three blank lines to space content out looks identical to typing
// one. Splice a zero-width space onto each blank line in the run so
// remark-parse no longer sees it as blank (keeping the whole run inside one
// paragraph); remarkBreaks then turns every individual "\n" into its own
// <br>, so the visual gap matches the newline count staff actually typed.
function preserveBlankLineRuns(markdown: string): string {
  return markdown.replace(/\n{2,}/g, (run) => run.split('\n').join('\n​'))
}

// Tailwind's Preflight resets ul/ol to list-style:none and strips their
// padding, so a rendered <ul> looks identical to plain text. Re-apply the
// list markers and indentation here (arbitrary-variant utilities — no
// @tailwindcss/typography in this project). Every Markdown consumer gets
// this; per-call wrappers only tweak margins.
const LIST_STYLES = '[&_ul]:list-disc [&_ul]:pl-5 [&_ol]:list-decimal [&_ol]:pl-5 [&_li]:my-0.5'

/**
 * Renders the weekplan / SFO free-text markdown with the shared restricted
 * tag allowlist. Same rendering on parent views, print views and cell previews.
 */
export function Markdown({ children }: { children: string | null | undefined }) {
  if (!children) {
    return null
  }
  return (
    <div className={LIST_STYLES}>
      <ReactMarkdown
        remarkPlugins={REMARK_PLUGINS}
        allowedElements={MD_ALLOWED_TAGS}
        unwrapDisallowed
      >
        {preserveBlankLineRuns(children)}
      </ReactMarkdown>
    </div>
  )
}
