interface LogoProps {
  /** 'dark' = on dark sidebar background (white strokes), 'light' = on white/light background (brand-color strokes) */
  variant?: 'dark' | 'light'
  size?: number
}

export default function Logo({ variant = 'light', size = 32 }: LogoProps) {
  const color = variant === 'dark' ? 'white' : '#1f6321'
  const gridColor = variant === 'dark' ? 'rgba(255,255,255,0.4)' : '#7db87d'

  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 32 32"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
      aria-hidden="true"
    >
      {/* Roof */}
      <polyline
        points="2,14 16,3 30,14"
        stroke={color}
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      {/* House body */}
      <rect x="5" y="14" width="22" height="16" rx="1.5" stroke={color} strokeWidth="2" />
      {/* Grid — vertical lines */}
      <line x1="12" y1="14" x2="12" y2="30" stroke={gridColor} strokeWidth="1.2" />
      <line x1="20" y1="14" x2="20" y2="30" stroke={gridColor} strokeWidth="1.2" />
      {/* Grid — horizontal lines */}
      <line x1="5" y1="19" x2="27" y2="19" stroke={gridColor} strokeWidth="1.2" />
      <line x1="5" y1="24" x2="27" y2="24" stroke={gridColor} strokeWidth="1.2" />
    </svg>
  )
}
