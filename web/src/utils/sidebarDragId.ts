export function encodeSidebarDragId(courseId: string) {
  return `sidebar-course:${courseId}`
}

export function decodeSidebarDragId(id: string): string | null {
  if (id.startsWith('sidebar-course:')) return id.slice('sidebar-course:'.length)
  return null
}
