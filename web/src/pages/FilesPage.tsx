import { usePageTitle } from '../hooks/usePageTitle'
import { FileSystemBrowser } from '../components/files/FileSystemBrowser'

export default function FilesPage() {
  usePageTitle('Filer')

  return (
    <div className="p-6 lg:p-8 max-w-5xl mx-auto">
      <FileSystemBrowser />
    </div>
  )
}
