# MODEL_INTEGRATION — memasukkan model ROV ke simulator

Sumber: arsip `RoV_Content` (4 FBX + tekstur Tripo, sudah ada .meta). Kondisi yang diketahui:
- Poligon sangat tinggi: sekitar 300 ribu vertex per model (Mariner XL sekitar 60 ribu). Terlalu berat untuk dipakai langsung di simulator.
- Tekstur per model: `Color.jpg`, `Normal.png`, `*_metallic.JPEG`, `*_roughness.JPEG` (folder `*.fbm`). Mariner XL hanya 512 px dan tanpa normal map.
- **Model "Teledyne" adalah glider Slocum, bukan ROV.** Jangan dipakai sebagai Teledyne sampai tipe Teledyne dikonfirmasi Pushidrosal. Pakai sebagai placeholder berlabel jelas.

## Aturan umum
1. Satu prefab per ROV di `Assets/_Falah/Content/Rov/<Nama>/` dengan struktur anak:
   `Visual` (mesh), `Colliders` (primitif), `Thrusters/T01..Tnn` (Transform kosong, arah dari profil), `Cameras/Main`, `Lights`, `Sensors/Sonar`.
2. Satuan meter, sumbu depan = +Z, atas = +Y. Pivot di titik tengah massa (pusat daya apung diatur dari profil). Koreksi skala/rotasi di import settings, bukan di prefab.
3. Collider memakai primitif (box/capsule gabungan). Jangan MeshCollider dari mesh asli.
4. Setelan import: Read/Write off, Generate Colliders off, Optimize Mesh on, Mesh Compression sedang. Tekstur Normal diset tipe Normal Map. Color diset sRGB, sisanya Linear.
5. Mesh mentah tidak masuk build. Versi simulasi memakai LOD (LOD0 maksimal 50 ribu vertex, LOD1 15 ribu, LOD2 5 ribu).
6. Parameter fisika, posisi thruster, dan dimensi dibaca dari `RovProfile`, bukan dari mesh. Mesh hanya tampilan.

## Material
| Pipeline | BaseColor | Normal | Metallic/Smoothness |
|---|---|---|---|
| HDRP Lit | Color | Normal | MaskMap: R=Metallic, G=AO (putih bila tidak ada), B=0, A=Smoothness (= 1 − Roughness) |
| URP Lit | Color | Normal | Metallic map: R=Metallic, A=Smoothness (= 1 − Roughness); Smoothness Source = Metallic Alpha |

Karena Metallic dan Roughness terpisah di arsip, tulis alat editor `RovTexturePacker` (menu Falah/Rov/Pack Textures) yang membaca keduanya dan menulis tekstur gabungan sesuai pipeline (lihat STATE.md). Alat memakai `Texture.isReadable` sementara, menulis PNG ke `Content/Rov/<Nama>/Textures/`, lalu mengembalikan setelan import. Jangan edit tekstur asli.

## Per model
| Model | Dipakai sebagai | Catatan |
|---|---|---|
| Tortuga | Tortuga (ROV kecil, prototype) | Siap setelah LOD. Konfirmasi varian (STD/XP4) dan sonar. |
| ECA Hytech | ECA Hytech (work class, fase 5) | Konfirmasi H300/H800/H1000 dan manipulator sebelum thruster dan profil dikunci. |
| Mariner XL | Mariner XL (work class, fase 5) | Tekstur lemah: minta tekstur ulang atau buat ulang tekstur di Substance/Blender. Konfirmasi manipulator, kedalaman, LARS. |
| "Teledyne" (glider) | Placeholder saja | Ganti dengan model ROV Teledyne yang benar. Jangan tulis spesifikasi glider ke profil Teledyne. |

## Pekerjaan MANUAL (Unity Artist)
- Retopologi/decimate di Blender sampai target LOD; bake normal dari mesh asli ke mesh rendah.
- Tekstur ulang Mariner XL (minimal 2K, dengan normal map).
- Model Teledyne yang benar, serta lengan manipulator terpisah (bone/joint) untuk ECA dan Mariner.
- Periksa lisensi aset Tripo untuk penggunaan komersial/serah terima.
