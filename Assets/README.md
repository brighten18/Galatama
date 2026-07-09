# README Galatama

## Tentang Game

**Galatama** adalah game eksplorasi dan edukasi budidaya ikan hias berbasis Unity. Pemain memulai permainan dengan mengikuti alur cerita singkat, lalu menjalankan rangkaian misi yang membawa pemain dari tahap pengenalan area, membaca informasi edukatif, menangkap ikan, memindahkan ikan ke akuarium, menjaga kualitas air, hingga menyelesaikan kuis untuk membuka hadiah.

Secara garis besar, tujuan permainan adalah:

1. Menyelesaikan misi utama secara berurutan.
2. Mempelajari dasar interaksi budidaya ikan hias melalui poster, notes, dan kuis.
3. Menangkap dan mengelola ikan di dalam akuarium.
4. Menjaga ikan tetap hidup dengan manajemen kualitas air dan pemberian pakan.
5. Setelah misi utama selesai, pemain bebas melanjutkan eksplorasi dan budidaya ikan.

---

## Fitur Utama

### 1. Eksplorasi Third-Person
- Kamera third-person dengan kontrol mouse.
- Karakter dapat berjalan, berlari, melompat, dan berinteraksi dengan objek di dunia.
- Ada sistem highlight dan prompt interaksi saat pemain mengarahkan pandangan ke objek penting.

### 2. Alur Cerita dan Monolog
- Permainan dibuka dengan cutscene dan monolog pembuka.
- Ada notes dan percakapan monolog yang mengarahkan pemain memahami konteks permainan.
- Tutorial dan panel petunjuk muncul pada momen tertentu untuk membantu pemain.

### 3. Sistem Misi Bertahap
Game memiliki rangkaian misi utama yang berjalan berurutan:

1. Baca notes di meja kamar.
2. Pergi ke area gudang.
3. Baca poster di mading gudang.
4. Ambil jaring/serokan di depan gudang.
5. Tangkap ikan hias menggunakan jaring.
6. Taruh ikan hias ke dalam akuarium.
7. Penuhi Akuarium 1 dengan 9 ikan dan pertahankan agar tetap hidup.
8. Temui Pa Kumis dan kerjakan kuis untuk mendapat hadiah.
9. Setelah misi utama selesai, pemain masuk fase bebas untuk eksplorasi dan budidaya sesuka hati.

### 4. Sistem Inventori dan Quick Slot
- Item yang diambil masuk ke hotbar/quick slot lebih dulu jika masih kosong.
- Tersedia inventori utama untuk menyimpan item tambahan.
- Pemain bisa memilih item cepat lewat tombol angka `1` sampai `6`.
- Inventori mendukung drag-and-drop.

### 5. Penangkapan Ikan
- Ikan dapat ditangkap langsung menggunakan jaring.
- Tersedia juga sistem perangkap ikan yang bisa dipasang di area laut.
- Perangkap dapat menangkap ikan secara otomatis setelah beberapa waktu.
- Ikan yang tertangkap masuk ke inventori dan dapat dibawa ke akuarium.

### 6. Sistem Akuarium dan Budidaya
- Pemain dapat membuka panel akuarium dan memindahkan ikan dari inventori ke slot akuarium.
- Akuarium memiliki batas kapasitas ikan.
- Ikan di akuarium mempunyai status:
  - lapar/kenyang,
  - sehat/stress,
  - hidup/mati.
- Ikan dapat dipindahkan kembali dari akuarium ke inventori.

### 7. Simulasi Kualitas Air
Akuarium tidak hanya menjadi tempat penyimpanan ikan, tetapi juga memiliki simulasi kualitas air yang aktif:

- **Oksigen (DO)**
- **Amonia**
- **Temperatur**
- **pH**
- **Salinitas**

Perubahan parameter air memengaruhi kelangsungan hidup ikan. Jika kondisi air memburuk, ikan bisa:

- menjadi stress,
- kehilangan health,
- mati karena oksigen rendah,
- mati karena kualitas air buruk,
- mati jika tidak diberi makan terlalu lama.

### 8. Peralatan Perawatan Akuarium
Pemain dapat menggunakan atau memasang berbagai alat/perlengkapan, seperti:

- aerator,
- heater,
- cooler/chiller,
- water pump,
- penambah air,
- pH buffer,
- pengurang amonia,
- garam,
- pelet/pakan ikan.

Fungsinya antara lain:

- menaikkan oksigen,
- menstabilkan pH,
- menurunkan amonia,
- mengubah salinitas,
- mengatur suhu,
- memberi makan ikan.

### 9. Kuis Edukasi
- Terdapat sistem kuis berbentuk beberapa gelombang.
- Setiap wave mengambil kumpulan soal dan mengacak urutan jawaban.
- Pemain harus mencapai nilai minimum agar lulus.
- Saat wave tertentu lulus, hadiah/interaksi tertentu akan terbuka.

### 10. Save Slot dan Save Point
- Tersedia **4 slot save**.
- Pemain bisa memulai game baru di slot tertentu, memuat save, rename save, dan menghapus save.
- Progress permainan dapat disimpan melalui **save point** di dalam game.
- Data yang tersimpan mencakup:
  - posisi pemain,
  - inventori,
  - progres misi,
  - progres kuis,
  - status tutorial,
  - status monolog,
  - isi akuarium,
  - kualitas air,
  - perangkap yang sudah dipasang.

### 11. Pause Menu dan Settings
- Pause menu tersedia di dalam game.
- Pengaturan yang tersedia:
  - kualitas grafis: Low, Medium, High,
  - resolusi layar,
  - mode fullscreen,
  - toggle LOD,
  - volume musik,
  - volume SFX.

---

## Cara Instalasi dan Menjalankan Game

## Opsi A - Jika yang Anda punya adalah file ZIP build game

1. Ekstrak file ZIP ke folder mana saja.
2. Pastikan seluruh isi ZIP tetap berada dalam satu folder yang sama setelah diekstrak.
3. Jalankan file `.exe` utama game.
4. Jika Windows menampilkan peringatan keamanan:
   - pilih `More info`,
   - lalu pilih `Run anyway` jika Anda yakin file berasal dari sumber yang aman.
5. Setelah game terbuka, masuk ke menu utama lalu pilih:
   - `Start` untuk membuat permainan baru,
   - `Load Game` untuk memuat progres yang sudah ada.

Catatan:
- Jangan memindahkan file `.exe` sendirian tanpa folder data pendukungnya.
- Save game biasanya disimpan otomatis di folder data pengguna Windows melalui sistem `persistentDataPath` Unity.

## Opsi B - Jika yang Anda punya adalah source project Unity

Project ini dibuat dengan:

- **Unity 6.2.10f1**
- Input System aktif
- URP (Universal Render Pipeline)

Langkah membuka project:

1. Buka **Unity Hub**.
2. Tambahkan folder project `Galatama`.
3. Pastikan editor yang dipakai adalah **Unity 6.2.10f1**.
4. Buka project, lalu jalankan scene menu utama atau scene gameplay sesuai kebutuhan.

Scene yang tersedia:

- `MainMenu`
- `LoadingScene`
- `CutScene`
- `Galatama`
- `Environment`

---

## Kontrol Permainan

Kontrol utama keyboard dan mouse:

- `W A S D` / `Arrow Keys`: bergerak
- `Mouse`: menggerakkan kamera
- `Left Shift`: sprint / lari
- `Space`: lompat
- `E`: interaksi utama dengan objek, notes, poster, save point, dan panel interaksi
- `Klik Kiri`: gunakan item aktif / interaksi objek sekunder
- `I`: buka/tutup inventori
- `1` `2` `3` `4` `5` `6`: pilih quick slot
- `Esc`: pause menu
- `Ctrl`: tampilkan/sembunyikan kursor bebas
- `Klik Kanan` saat kursor tampil: tahan untuk rotasi kamera sementara

Penjelasan singkat:

- `E` dipakai untuk berinteraksi dengan objek dunia seperti notes, poster, save point, pintu, akuarium, dan objek pickup.
- `Klik Kiri` dipakai untuk aksi item yang sedang dipegang, misalnya memakai jaring atau menempatkan perangkap.
- Saat inventori atau panel tertentu dibuka, gerakan pemain akan dikunci sementara.

---

## Cara Bermain

## Awal Permainan

1. Mulai dari menu utama.
2. Pilih slot save untuk membuat permainan baru.
3. Tonton cutscene pembuka.
4. Ikuti monolog dan tutorial awal.

## Progres Inti

1. Ikuti panel misi yang tampil di layar.
2. Datangi waypoint atau area tujuan.
3. Baca notes dan poster untuk melanjutkan progres.
4. Ambil jaring.
5. Tangkap ikan hias.
6. Buka akuarium lalu pindahkan ikan ke dalamnya.
7. Tambahkan ikan lain hingga target terpenuhi.
8. Jaga kualitas air dan beri makan ikan agar tidak mati.
9. Selesaikan kuis untuk membuka hadiah/reward.

## Setelah Misi Selesai

Setelah misi utama berakhir, permainan masuk ke mode bebas. Pada tahap ini pemain dapat:

- menangkap ikan lagi,
- menaruh ikan ke akuarium,
- bereksperimen dengan alat perawatan air,
- mengelola isi akuarium,
- melanjutkan eksplorasi area permainan.

---

## Mekanik Penting yang Harus Dipahami

### Menangkap Ikan
- Equip jaring dari quick slot.
- Dekati ikan.
- Gunakan aksi item untuk menangkap ikan.
- Jika inventori penuh, ikan tidak dapat diambil.

### Menggunakan Perangkap
- Equip perangkap dari quick slot.
- Gunakan aksi item di area dasar laut yang valid.
- Tunggu beberapa saat sampai perangkap menangkap ikan.
- Ambil kembali perangkap untuk memperoleh item perangkap dan ikan hasil tangkapan.

### Mengelola Akuarium
- Buka akuarium melalui interaksi.
- Pindahkan ikan dari inventori ke akuarium.
- Pantau indikator kualitas air.
- Gunakan alat/perawatan bila parameter air memburuk.
- Beri pakan agar ikan tidak kelaparan.

### Menjaga Ikan Tetap Hidup
Perhatikan hal-hal berikut:

- Oksigen terlalu rendah berbahaya.
- Amonia tinggi merusak kondisi air.
- Suhu terlalu rendah atau terlalu tinggi memengaruhi sistem air.
- pH di luar batas aman mengganggu stabilitas.
- Salinitas harus dijaga sesuai rentang aman.
- Ikan yang tidak diberi makan dapat mati.

---

## Daftar Elemen Konten yang Terlihat di Proyek

### Jenis ikan yang tersedia
Berdasarkan asset yang ada di proyek, beberapa ikan yang digunakan antara lain:

- Banggai
- Blenny
- Blue Tang
- Chromis Blue
- Clown Fish
- Dotty Back
- Fire Fish
- Mandarin
- Royal Gramma
- Yellow Tang

### Item/peralatan yang tersedia
- Jaring
- Perangkap
- Pelet
- Garam
- pH Buffer
- Amonia Remover
- Aerator
- Heater
- Cooler/Chiller
- Water Pump
- Penambah air

---

## Sistem Save

Game ini memakai sistem save berbasis slot. Yang perlu diketahui pemain:

- Slot kosong bisa langsung dipakai untuk permainan baru.
- Slot yang sudah terisi bisa di-load, di-rename, atau dihapus.
- Save dilakukan melalui save point di dalam permainan.
- Data progres tersimpan per slot.

Saran:
- Gunakan save point setelah menyelesaikan misi penting.
- Simpan progres sebelum mencoba eksperimen besar pada akuarium.

---

## Tips Bermain

- Ikuti misi aktif lebih dulu agar semua fitur terbuka secara bertahap.
- Pakai quick slot untuk mempercepat perpindahan alat.
- Jangan biarkan ikan terlalu lama tanpa pakan.
- Perhatikan indikator kualitas air, bukan hanya jumlah ikan.
- Jika suatu area atau interaksi terkunci, kemungkinan Anda perlu menyelesaikan kuis atau misi lebih dulu.

---

## Troubleshooting

### Game tidak bisa dijalankan setelah ekstrak ZIP
- Pastikan file `.exe` dan folder data game tidak terpisah.
- Coba jalankan dari folder hasil ekstrak, bukan dari dalam aplikasi arsip.

### Save tidak muncul
- Pastikan Anda sudah menyimpan melalui save point.
- Cek apakah Anda memuat slot yang sama dengan slot saat menyimpan.

### Interaksi terasa tidak jalan
- Pastikan kursor tidak sedang aktif bebas jika Anda ingin kembali mengontrol kamera penuh.
- Dekatkan karakter ke objek dan arahkan kamera ke objek hingga prompt interaksi muncul.
- Pastikan inventori, pause, tutorial, atau kuis tidak sedang terbuka.

### Ikan mati terus
- Cek oksigen, amonia, suhu, pH, dan salinitas.
- Beri pakan secara berkala.
- Gunakan alat perawatan akuarium sesuai kebutuhan.

---

## Ringkasan Tujuan Game

Jika dijelaskan secara singkat, tujuan bermain **Galatama** adalah:

- belajar dan menjelajahi lingkungan permainan,
- menyelesaikan rangkaian misi edukatif,
- menangkap ikan hias,
- memelihara ikan di akuarium dengan kondisi air yang stabil,
- membuka reward lewat kuis,
- lalu melanjutkan budidaya dan eksplorasi secara bebas.

---

## Lokasi File README

README ini disimpan di:

`Assets/README.md`

