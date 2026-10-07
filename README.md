# ROV Trainer Simulator (Unity)

Prototype simulator pelatihan operator ROV, Pushidrosal TNI AL. Software Development Division, Falah Inovasi Teknologi.
Mulai dari `CLAUDE.md` dan `docs/`.

## Aset yang TIDAK ada di repo ini
Aset berlisensi/pihak ketiga dan model sumber tidak ikut di-commit. Impor sendiri ke folder yang sama agar scene tidak kehilangan referensi:

| Folder | Isi |
|---|---|
| `Assets/KriptoFX/` | KriptoFX Water System (Asset Store) |
| `Assets/WaterWorks/` | WaterWorks |
| `Assets/AllSkyFree/` | AllSkyFree skybox |
| `Assets/Plugins/Easy performant outline/` | Easy Performant Outline (Asset Store) |
| `Assets/3D Contents/` | Model ROV, Ferry, suitcase, NPC (Tripo, lisensi perlu dikonfirmasi) |

Zenject (`Assets/Plugins/Zenject`, MIT) ikut di repo.

## Data peta
Tile dasar laut dilayani server lokal (nginx), bukan Cesium ion cloud. Lihat `docs/STATE.md`.
