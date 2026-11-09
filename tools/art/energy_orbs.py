"""Draw the energy orb icons (big_energy 74px, text_energy 24px) for each character as SVG and render them to PNG.

Usage: python3 tools/art/energy_orbs.py   (needs Node + Playwright with Chromium)
"""
import os, subprocess, tempfile

ORBS = {
    "Golem": """
  <radialGradient id="g1" cx="35%" cy="30%" r="75%"><stop offset="0" stop-color="#b3a28c"/><stop offset="0.55" stop-color="#7a6650"/><stop offset="1" stop-color="#2e241a"/></radialGradient>
  <radialGradient id="glow" cx="50%" cy="50%" r="50%"><stop offset="0" stop-color="#ffd27a"/><stop offset="0.5" stop-color="#ff9a30" stop-opacity="0.7"/><stop offset="1" stop-color="#ff6a00" stop-opacity="0"/></radialGradient>
  </defs>
  <circle cx="50" cy="50" r="49" fill="url(#glow)"/>
  <path d="M50 8 L78 16 L92 44 L86 76 L58 92 L26 88 L9 62 L12 30 Z" fill="url(#g1)" stroke="#1e160e" stroke-width="5" stroke-linejoin="round"/>
  <path d="M26 30 L36 38 L33 48 M64 22 L60 32 L66 40 M44 74 L54 70 L58 80" stroke="#ffa63a" stroke-width="5" fill="none" stroke-linecap="round"/>
  <path d="M10 52 Q50 70 90 52" stroke="#8a4a22" stroke-width="8" fill="none"/>
  <circle cx="30" cy="59" r="3" fill="#e0a060"/><circle cx="70" cy="59" r="3" fill="#e0a060"/>
  <ellipse cx="36" cy="26" rx="12" ry="6" fill="#fff" opacity="0.25" transform="rotate(-25 36 26)"/>""",
    "CardSharp": """
  <linearGradient id="gold" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#ffe9a0"/><stop offset="0.5" stop-color="#d4a840"/><stop offset="1" stop-color="#8a6418"/></linearGradient>
  <radialGradient id="blk" cx="35%" cy="30%" r="75%"><stop offset="0" stop-color="#4a4644"/><stop offset="1" stop-color="#141212"/></radialGradient>
  </defs>
  <circle cx="50" cy="50" r="46" fill="url(#blk)" stroke="#0a0808" stroke-width="5"/>
  <circle cx="50" cy="50" r="40" fill="none" stroke="url(#gold)" stroke-width="11" stroke-dasharray="15.7 15.7"/>
  <circle cx="50" cy="50" r="27" fill="url(#blk)" stroke="url(#gold)" stroke-width="4"/>
  <path d="M50 34 C38 44 36 51 43 55 C46 57 48 55 49 53 L46 62 L54 62 L51 53 C52 55 54 57 57 55 C64 51 62 44 50 34 Z" fill="url(#gold)"/>
  <ellipse cx="34" cy="26" rx="12" ry="5" fill="#fff" opacity="0.2" transform="rotate(-30 34 26)"/>""",
    "Bard": """
  <radialGradient id="v" cx="35%" cy="30%" r="75%"><stop offset="0" stop-color="#b48ae8"/><stop offset="0.55" stop-color="#5b3c9e"/><stop offset="1" stop-color="#1e1040"/></radialGradient>
  <radialGradient id="glow" cx="50%" cy="50%" r="50%"><stop offset="0" stop-color="#fff0b0"/><stop offset="0.5" stop-color="#ffd060" stop-opacity="0.5"/><stop offset="1" stop-color="#ffb030" stop-opacity="0"/></radialGradient>
  <linearGradient id="gold" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#ffe9a0"/><stop offset="1" stop-color="#b08020"/></linearGradient>
  </defs>
  <circle cx="50" cy="50" r="49" fill="url(#glow)"/>
  <circle cx="50" cy="50" r="40" fill="url(#v)" stroke="url(#gold)" stroke-width="6"/>
  <g fill="#ffd060"><ellipse cx="42" cy="64" rx="10" ry="7.5" transform="rotate(-25 42 64)"/><rect x="49" y="30" width="5" height="34"/><path d="M54 30 q16 6 13 22 q-4 -12 -13 -12z"/></g>
  <ellipse cx="34" cy="28" rx="12" ry="5" fill="#fff" opacity="0.3" transform="rotate(-30 34 28)"/>""",
    "Falconer": """
  <radialGradient id="l" cx="35%" cy="30%" r="75%"><stop offset="0" stop-color="#d9a066"/><stop offset="0.55" stop-color="#8a5a2b"/><stop offset="1" stop-color="#2e1c0c"/></radialGradient>
  <radialGradient id="glow" cx="50%" cy="50%" r="50%"><stop offset="0" stop-color="#fff0b0"/><stop offset="0.5" stop-color="#ffc050" stop-opacity="0.5"/><stop offset="1" stop-color="#ff9a30" stop-opacity="0"/></radialGradient>
  </defs>
  <circle cx="50" cy="50" r="49" fill="url(#glow)"/>
  <circle cx="50" cy="50" r="40" fill="url(#l)" stroke="#2a1a0c" stroke-width="5"/>
  <path d="M30 72 C40 50 52 34 72 26 C68 44 56 60 34 74 Z" fill="#f2e2c0" stroke="#3a2610" stroke-width="3"/>
  <path d="M33 72 L66 32" stroke="#3a2610" stroke-width="2.5"/>
  <ellipse cx="34" cy="28" rx="12" ry="5" fill="#fff" opacity="0.3" transform="rotate(-30 34 28)"/>""",
}

JS = """
const { chromium } = require('playwright');
(async () => {
  const b = await chromium.launch(); const p = await b.newPage();
  for (const [svg, png, size] of JSON.parse(process.argv[2])) {
    await p.setViewportSize({ width: size, height: size });
    await p.goto('file://' + svg);
    await p.screenshot({ path: png, omitBackground: true });
  }
  await b.close();
})();
"""

def main():
    root = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
    tmp = tempfile.mkdtemp()
    jobs = []
    for mod, body in ORBS.items():
        for name, size in (("big_energy", 74), ("text_energy", 24)):
            svg = os.path.join(tmp, f"{mod}_{name}.svg")
            with open(svg, "w") as f:
                f.write(f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100" width="{size}" height="{size}"><defs>{body}</svg>')
            jobs.append([svg, os.path.join(root, mod, mod, "images", "charui", f"{name}.png"), size])
        with open(os.path.join(root, "tools", "art", "reference", f"energy_{mod.lower()}.svg"), "w") as f:
            f.write(f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100" width="200" height="200"><defs>{body}</svg>')
    js = os.path.join(tmp, "r.js"); open(js, "w").write(JS)
    import json
    env = dict(os.environ, NODE_PATH=subprocess.check_output(["npm", "root", "-g"], text=True).strip())
    subprocess.check_call(["node", js, json.dumps(jobs)], env=env)
    for j in jobs: print("wrote", os.path.relpath(j[1], root))

if __name__ == "__main__":
    main()
