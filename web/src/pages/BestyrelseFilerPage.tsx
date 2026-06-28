import { usePageTitle } from '../hooks/usePageTitle'
import { FileSystemBrowser } from '../components/files/FileSystemBrowser'

export default function BestyrelseFilerPage() {
  usePageTitle('Bestyrelsesdokumenter')

  return (
    <div className="p-6 lg:p-8 max-w-5xl mx-auto">
      <FileSystemBrowser variant="board" />
    </div>
  )
}
