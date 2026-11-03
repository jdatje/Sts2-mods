"""Generate Strike card mockups (STS2 card layout) for Golem, Card Sharp and Bard as SVG."""

W, H = 660, 915  # 3x the in-game card

FRAME = """
  <!-- outer frame -->
  <rect x="6" y="6" width="648" height="903" rx="30" fill="{frame_dark}"/>
  <rect x="16" y="16" width="628" height="883" rx="24" fill="url(#frameGrad)"/>
  <rect x="16" y="16" width="628" height="883" rx="24" fill="url(#frameTex)" opacity="0.35"/>
  <rect x="34" y="34" width="592" height="847" rx="14" fill="none" stroke="{frame_dark}" stroke-width="4" opacity="0.6"/>

  <!-- art window: rectangle with a pointed bottom that meets the type plaque -->
  <path id="win" d="M60 130 L600 130 L600 420 L380 480 L280 480 L60 420 Z" fill="{frame_dark}"/>
  <clipPath id="artClip"><path d="M70 140 L590 140 L590 412 L378 470 L282 470 L70 412 Z"/></clipPath>
  <g clip-path="url(#artClip)">
    <rect x="60" y="130" width="540" height="350" fill="url(#artBg)"/>
    {art}
    <rect x="60" y="130" width="540" height="350" fill="url(#artVig)"/>
  </g>
  <path d="M70 140 L590 140 L590 412 L378 470 L282 470 L70 412 Z" fill="none" stroke="{trim}" stroke-width="7"/>
  <path d="M70 140 L590 140 L590 412 L378 470 L282 470 L70 412 Z" fill="none" stroke="{frame_dark}" stroke-width="2" opacity="0.7"/>

  <!-- description panel -->
  <rect x="70" y="520" width="520" height="355" rx="10" fill="{panel}"/>
  <rect x="70" y="520" width="520" height="355" rx="10" fill="url(#frameTex)" opacity="0.18"/>
  <rect x="70" y="520" width="520" height="355" rx="10" fill="none" stroke="{frame_dark}" stroke-width="3" opacity="0.7"/>

  <!-- type plaque -->
  <path d="M262 462 L398 462 L410 482 L398 502 L262 502 L250 482 Z" fill="url(#plaque)" stroke="{plaque_edge}" stroke-width="3"/>
  <text x="330" y="490" text-anchor="middle" class="type">Attack</text>

  <!-- title banner -->
  <path d="M40 92 L20 62 L92 62 L92 112 Z" fill="{banner_dark}"/>
  <path d="M620 92 L640 62 L568 62 L568 112 Z" fill="{banner_dark}"/>
  <path d="M70 44 Q330 26 590 44 L600 106 Q330 90 60 106 Z" fill="url(#banner)" stroke="{banner_edge}" stroke-width="4"/>
  <path d="M78 54 Q330 38 582 54" stroke="#ffffff" stroke-width="2" fill="none" opacity="0.35"/>
  <text x="330" y="88" text-anchor="middle" class="title">Strike</text>

  <!-- description -->
  {desc}

  <!-- energy orb -->
  {orb}
"""

STYLE = """
  <style>
    .title {{ font: 700 44px 'Kreon','Georgia',serif; fill:#fff; stroke:#2a2420; stroke-width:7px; paint-order:stroke; letter-spacing:1px; }}
    .type  {{ font: 700 22px 'Kreon','Georgia',serif; fill:#3a3430; }}
    .desc  {{ font: 400 36px 'Kreon','Georgia',serif; fill:#f4efe6; stroke:#1a1410; stroke-width:5px; paint-order:stroke; }}
    .gold  {{ font: 700 32px 'Kreon','Georgia',serif; fill:#f2c94c; stroke:#1a1410; stroke-width:5px; paint-order:stroke; }}
    .cost  {{ font: 700 54px 'Kreon','Georgia',serif; fill:#fff; stroke:#1a1410; stroke-width:7px; paint-order:stroke; }}
  </style>
"""

COMMON_DEFS = """
  <filter id="glow" x="-50%" y="-50%" width="200%" height="200%"><feGaussianBlur stdDeviation="10"/></filter>
  <filter id="blur3"><feGaussianBlur stdDeviation="3"/></filter>
  <pattern id="frameTex" width="120" height="120" patternUnits="userSpaceOnUse">
    <rect width="120" height="120" fill="none"/>
    <path d="M0 30 Q30 20 60 34 T120 28 M0 80 Q40 70 70 86 T120 78" stroke="#000" stroke-width="1.5" fill="none" opacity="0.5"/>
    <circle cx="20" cy="55" r="2" fill="#000" opacity="0.4"/><circle cx="95" cy="105" r="2.5" fill="#000" opacity="0.4"/>
  </pattern>
  <radialGradient id="artVig" cx="50%" cy="45%" r="70%">
    <stop offset="0.65" stop-color="#000" stop-opacity="0"/><stop offset="1" stop-color="#000" stop-opacity="0.55"/>
  </radialGradient>
  <linearGradient id="plaque" x1="0" y1="0" x2="0" y2="1">
    <stop offset="0" stop-color="#e2ddd4"/><stop offset="1" stop-color="#a9a29a"/>
  </linearGradient>
"""

def card(c):
    defs = COMMON_DEFS + c["defs"] + f"""
  <linearGradient id="frameGrad" x1="0" y1="0" x2="1" y2="1">
    <stop offset="0" stop-color="{c['frame_light']}"/><stop offset="1" stop-color="{c['frame']}"/>
  </linearGradient>
  <linearGradient id="banner" x1="0" y1="0" x2="0" y2="1">
    <stop offset="0" stop-color="{c['banner_light']}"/><stop offset="1" stop-color="{c['banner']}"/>
  </linearGradient>"""
    body = FRAME.format(**c)
    return (f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {W} {H}" width="{W}" height="{H}">\n'
            f'<defs>{STYLE.format()}{defs}\n</defs>{body}</svg>\n')


# ---------------------------------------------------------------- GOLEM
golem = dict(
    frame="#5a4634", frame_light="#8a6e52", frame_dark="#2a1f16", trim="#b0714a",
    panel="#3a3129", banner="#6e6a62", banner_light="#a6a198", banner_dark="#4a4640", banner_edge="#2e2a26",
    plaque_edge="#5a5248",
    defs="""
  <radialGradient id="artBg" cx="55%" cy="40%" r="75%">
    <stop offset="0" stop-color="#d9893a"/><stop offset="0.35" stop-color="#8a4a22"/><stop offset="1" stop-color="#2a1a10"/>
  </radialGradient>
  <radialGradient id="rock" cx="35%" cy="30%" r="80%">
    <stop offset="0" stop-color="#b3a390"/><stop offset="0.5" stop-color="#7a6a58"/><stop offset="1" stop-color="#3a3026"/>
  </radialGradient>
  <linearGradient id="iron" x1="0" y1="0" x2="0" y2="1">
    <stop offset="0" stop-color="#c0703a"/><stop offset="1" stop-color="#5a2a12"/>
  </linearGradient>
  <radialGradient id="ember" cx="50%" cy="50%" r="50%">
    <stop offset="0" stop-color="#fff3b8"/><stop offset="0.4" stop-color="#ffaa30"/><stop offset="1" stop-color="#ff6a00" stop-opacity="0"/>
  </radialGradient>
  <radialGradient id="orbStone" cx="35%" cy="30%" r="75%">
    <stop offset="0" stop-color="#a49482"/><stop offset="1" stop-color="#3a3026"/>
  </radialGradient>""",
    art="""
    <!-- orange slash arc, like the Ironclad strike swoosh -->
    <path d="M90 420 Q200 170 560 190 Q260 230 150 430 Z" fill="#ffb347" opacity="0.85"/>
    <path d="M110 420 Q220 205 540 200 Q270 250 165 430 Z" fill="#ffe08a" opacity="0.7"/>
    <!-- clay forearm coming in from the lower left -->
    <path d="M40 470 L170 330 Q205 300 240 330 L150 480 Z" fill="#7a5539"/>
    <path d="M60 470 L175 345" stroke="#4a3020" stroke-width="6" opacity="0.6"/>
    <!-- rusted iron wrist band -->
    <path d="M150 352 L222 300 L250 334 L178 388 Z" fill="url(#iron)"/>
    <circle cx="178" cy="356" r="5" fill="#e0a060"/><circle cx="214" cy="330" r="5" fill="#e0a060"/>
    <!-- boulder fist -->
    <ellipse cx="330" cy="255" rx="120" ry="100" fill="url(#rock)" transform="rotate(-25 330 255)"/>
    <path d="M250 230 Q300 200 350 210 M265 270 Q320 240 380 250 M285 310 Q340 285 400 292" stroke="#2e261e" stroke-width="7" fill="none" opacity="0.7"/>
    <ellipse cx="300" cy="200" rx="45" ry="20" fill="#7c8a4a" opacity="0.7" transform="rotate(-25 300 200)"/>
    <!-- glowing cracks -->
    <path d="M360 190 L380 230 L365 260 L392 290" stroke="#ffa63a" stroke-width="6" fill="none"/>
    <path d="M300 300 L330 280 L350 300" stroke="#ffa63a" stroke-width="5" fill="none"/>
    <!-- impact burst + flying shards -->
    <circle cx="460" cy="190" r="70" fill="url(#ember)" filter="url(#glow)"/>
    <g fill="url(#rock)">
      <path d="M470 160 l28 -18 l18 16 l-14 24 z"/><path d="M500 230 l24 -6 l8 20 l-22 8 z"/>
      <path d="M430 135 l16 -14 l14 10 l-10 16 z"/><path d="M530 175 l20 -4 l6 16 l-18 6 z"/>
    </g>
    <g fill="#4a3020"><circle cx="545" cy="140" r="6"/><circle cx="510" cy="270" r="5"/><circle cx="450" cy="250" r="4"/></g>
    """,
    desc='<text x="330" y="712" text-anchor="middle" class="desc">Deal 6 damage.</text>',
    orb="""
    <circle cx="60" cy="62" r="50" fill="url(#ember)" opacity="0.8" filter="url(#glow)"/>
    <circle cx="60" cy="62" r="42" fill="url(#orbStone)" stroke="#1e1610" stroke-width="5"/>
    <path d="M24 52 L32 60 L26 72 M96 50 L90 58 L96 68" stroke="#ff9a30" stroke-width="3" fill="none"/>
    <path d="M22 70 Q60 92 98 70" stroke="url(#iron)" stroke-width="8" fill="none"/>
    <text x="60" y="81" text-anchor="middle" class="cost">1</text>""",
)

# ---------------------------------------------------------------- CARD SHARP
sharp = dict(
    frame="#1e4a32", frame_light="#3a7a52", frame_dark="#0c2016", trim="#d4a840",
    panel="#232a24", banner="#6a1a26", banner_light="#a83444", banner_dark="#4a1018", banner_edge="#2a0a10",
    plaque_edge="#8a7040",
    defs="""
  <radialGradient id="artBg" cx="45%" cy="45%" r="75%">
    <stop offset="0" stop-color="#3fae6a"/><stop offset="0.4" stop-color="#1c5a36"/><stop offset="1" stop-color="#08200f"/>
  </radialGradient>
  <linearGradient id="cardface" x1="0" y1="0" x2="1" y2="1">
    <stop offset="0" stop-color="#fffaf0"/><stop offset="1" stop-color="#e2d4b4"/>
  </linearGradient>
  <linearGradient id="goldEdge" x1="0" y1="0" x2="1" y2="1">
    <stop offset="0" stop-color="#ffe9a0"/><stop offset="1" stop-color="#b08020"/>
  </linearGradient>
  <radialGradient id="cardglow" cx="50%" cy="50%" r="50%">
    <stop offset="0" stop-color="#fff3c0"/><stop offset="1" stop-color="#ffc040" stop-opacity="0"/>
  </radialGradient>
  <g id="spade"><path d="M0 -14 C-14 -2 -16 6 -8 10 C-4 12 -2 10 -1 8 L-4 16 L4 16 L1 8 C2 10 4 12 8 10 C16 6 14 -2 0 -14 Z" fill="#1a1a1a"/></g>""",
    art="""
    <!-- gold slash streak -->
    <path d="M80 430 Q220 200 580 170 Q280 250 140 440 Z" fill="#f2c94c" opacity="0.8"/>
    <path d="M100 430 Q240 230 560 182 Q290 270 158 440 Z" fill="#fff0b0" opacity="0.65"/>
    <!-- trailing ghost cards -->
    <g opacity="0.25"><rect x="-38" y="-54" width="76" height="108" rx="7" fill="#fff" transform="translate(200 360) rotate(-60)"/></g>
    <g opacity="0.4"><rect x="-38" y="-54" width="76" height="108" rx="7" fill="#fff" transform="translate(280 300) rotate(-52)"/></g>
    <!-- the thrown card, edge-on like a blade -->
    <circle cx="410" cy="230" r="110" fill="url(#cardglow)" filter="url(#glow)"/>
    <g transform="translate(410 230) rotate(-42)">
      <rect x="-62" y="-88" width="124" height="176" rx="10" fill="url(#cardface)" stroke="url(#goldEdge)" stroke-width="6"/>
      <g transform="scale(3.6)"><use href="#spade"/></g>
      <text x="-44" y="-56" font-family="Georgia,serif" font-weight="700" font-size="30" fill="#1a1a1a">7</text>
      <text x="44" y="74" font-family="Georgia,serif" font-weight="700" font-size="30" fill="#1a1a1a" text-anchor="middle" transform="rotate(180 44 64)">7</text>
    </g>
    <!-- sparks / chips -->
    <g fill="#ffe39a"><circle cx="520" cy="160" r="5"/><circle cx="540" cy="250" r="4"/><circle cx="490" cy="320" r="3"/><circle cx="560" cy="200" r="3"/></g>
    <g transform="translate(130 200)"><ellipse rx="34" ry="12" fill="#8a1c28"/><ellipse cy="-7" rx="34" ry="12" fill="#c8303c"/><ellipse cy="-7" rx="20" ry="7" fill="none" stroke="#f2e6c8" stroke-width="3" stroke-dasharray="6 6"/></g>
    """,
    desc='<text x="330" y="680" text-anchor="middle" class="gold">7 of Spades</text>'
         '<text x="330" y="735" text-anchor="middle" class="desc">Deal 6 damage.</text>',
    orb="""
    <circle cx="60" cy="62" r="46" fill="#a01c28" stroke="#3a0a10" stroke-width="5"/>
    <circle cx="60" cy="62" r="46" fill="none" stroke="#f2e6c8" stroke-width="10" stroke-dasharray="14 14"/>
    <circle cx="60" cy="62" r="32" fill="#c8303c" stroke="#d4a840" stroke-width="4"/>
    <text x="60" y="81" text-anchor="middle" class="cost">1</text>""",
)

# ---------------------------------------------------------------- BARD
bard = dict(
    frame="#4a2266", frame_light="#7a46a0", frame_dark="#1e0c2a", trim="#d4a840",
    panel="#2c2034", banner="#6a1e30", banner_light="#a83850", banner_dark="#480f1e", banner_edge="#2a0812",
    plaque_edge="#8a7040",
    defs="""
  <radialGradient id="artBg" cx="50%" cy="40%" r="75%">
    <stop offset="0" stop-color="#c07ad8"/><stop offset="0.4" stop-color="#5a2a7a"/><stop offset="1" stop-color="#160820"/>
  </radialGradient>
  <linearGradient id="wood" x1="0" y1="0" x2="1" y2="1">
    <stop offset="0" stop-color="#d8984e"/><stop offset="1" stop-color="#6a3a18"/>
  </linearGradient>
  <radialGradient id="noteglow" cx="50%" cy="50%" r="50%">
    <stop offset="0" stop-color="#fff0b0"/><stop offset="1" stop-color="#ffb030" stop-opacity="0"/>
  </radialGradient>
  <radialGradient id="orbPurple" cx="35%" cy="30%" r="75%">
    <stop offset="0" stop-color="#c890e8"/><stop offset="1" stop-color="#3a1458"/>
  </radialGradient>
  <g id="note"><ellipse cx="0" cy="0" rx="11" ry="8" transform="rotate(-25)" fill="#ffd060"/><rect x="8" y="-38" width="4" height="38" fill="#ffd060"/><path d="M12 -38 q16 6 14 22 q-4 -12 -14 -12z" fill="#ffd060"/></g>""",
    art="""
    <!-- golden sound-wave slash -->
    <path d="M80 430 Q210 190 580 180 Q270 240 140 440 Z" fill="#ffd060" opacity="0.75"/>
    <path d="M100 430 Q230 220 560 190 Q280 260 158 440 Z" fill="#fff0b0" opacity="0.6"/>
    <path d="M150 440 Q260 280 520 240" stroke="#fff0b0" stroke-width="3" fill="none" opacity="0.8" stroke-dasharray="2 14" stroke-linecap="round"/>
    <!-- lute swung like a club -->
    <g transform="translate(330 270) rotate(-35)">
      <rect x="-14" y="-200" width="28" height="160" rx="4" fill="#7a4a20"/>
      <rect x="-22" y="-240" width="44" height="48" rx="8" fill="#5a3010"/>
      <circle cx="-26" cy="-228" r="5" fill="#e8d8b0"/><circle cx="26" cy="-228" r="5" fill="#e8d8b0"/><circle cx="-26" cy="-206" r="5" fill="#e8d8b0"/><circle cx="26" cy="-206" r="5" fill="#e8d8b0"/>
      <ellipse cx="0" cy="30" rx="88" ry="110" fill="url(#wood)" stroke="#3a1e0c" stroke-width="4"/>
      <circle cx="0" cy="0" r="26" fill="#2a1408"/>
      <circle cx="0" cy="0" r="32" fill="none" stroke="#d4a840" stroke-width="3"/>
      <rect x="-28" y="88" width="56" height="10" rx="3" fill="#3a1e0c"/>
      <g stroke="#f2e6c8" stroke-width="2"><path d="M-8 -230 L-8 92"/><path d="M0 -230 L0 92"/><path d="M8 -230 L8 92"/></g>
      <!-- crack: it's a battered lute -->
      <path d="M40 60 L58 80 L50 100" stroke="#3a1e0c" stroke-width="3" fill="none"/>
    </g>
    <!-- notes bursting from the impact -->
    <circle cx="480" cy="190" r="60" fill="url(#noteglow)" filter="url(#glow)"/>
    <g transform="translate(470 210) scale(1.6)"><use href="#note"/></g>
    <g transform="translate(540 270) scale(1.1)"><use href="#note"/></g>
    <g transform="translate(520 150) scale(0.9)"><use href="#note"/></g>
    <g transform="translate(130 210) scale(1.1)"><use href="#note"/></g>
    """,
    desc='<text x="330" y="712" text-anchor="middle" class="desc">Deal 6 damage.</text>',
    orb="""
    <circle cx="60" cy="62" r="52" fill="url(#noteglow)" opacity="0.7" filter="url(#glow)"/>
    <circle cx="60" cy="62" r="42" fill="url(#orbPurple)" stroke="#d4a840" stroke-width="5"/>
    <g transform="translate(86 34) scale(0.6)" opacity="0.6"><use href="#note"/></g>
    <text x="60" y="81" text-anchor="middle" class="cost">1</text>""",
)

if __name__ == "__main__":
    import sys
    out = sys.argv[1] if len(sys.argv) > 1 else "."
    for name, c in [("strike_golem", golem), ("strike_cardsharp", sharp), ("strike_bard", bard)]:
        open(f"{out}/{name}.svg", "w").write(card(c))
        print("wrote", name)
