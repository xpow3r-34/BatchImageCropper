# BatchImageCropper
Toplu fotoğraf kırpma uygulaması.

> **Güncel Sürüm: <span style="color:red;font-weight:bold">v1.4.9</span>**

## Özellikler

- Çoklu fotoğraflarda bağımsız olarak seçim alanı oluşturup kırpma işlemi uygulayabilirsiniz.
- Birden fazla fotoğraf atıp **Senkronize Et** seçeneğini açarsanız çalıştığınız fotoğraftaki kırpma alanı diğer fotoğraflara uygulanır. Serbest kırpma tüm fotoğraflarda **aynı göreli bölgeyi** seçer; **en-boy oranı kilitliyse** kırpmanın piksel oranı korunur, farklı en-boy oranına sahip fotoğraflarda dahi kırpma şekli bozulmaz. Senkronu kapattığınızda bağımsız düzenlemeye devam edebilirsiniz.
- **Meta Veriyi Koru** aktif olduğunda dosya zaman damgaları kaynak fotoğrafla aynı olur.
- Kırpılan alanlar varsayılan olarak kaynak dosyanın bulunduğu klasöre kaydedilir.
- **Orijinali Sil** seçeneği işaretliyken başarılı kırpma işleminden sonra kaynak (orijinal) dosyalar geri dönüşüm kutusuna atılır ve listeden kaldırılır.
- Az sayıda görselde önizleme boyutu araç çubuğundaki **"Önizleme" kaydırıcısıyla** ayarlanır (%%10 - %%100, varsayılan %%25).
- Araç çubuğu **tek satır yatay** düzendedir; bölücü/bant çizgileri yoktur, hiçbir düğme taşma menüsüne düşmez.
- Sürükle-bırak ile resim ekleme, `Delete` tuşu ile seçili resmi kaldırma.
- Çok sayıda fotoğraf güvenle yüklenir: görüntü çözme arka planda yapılır, arayüz donmaz ve yükleme sırasında çökme yaşanmaz.
- TR / EN dil desteği.

## En-Boy Oranı

- Araç çubuğundaki **Oran** açılır listesinden hassas en-boy oranı seçilebilir: Serbest, 1:1, 4:3, 3:2, 16:9, 5:4, 21:9, 3:4, 2:3, 9:16.
- Oran seçiliyken çizilen seçim alanı belirtilen orana **birebir uyar**; başlangıç köşesinden imleç yönünde, görüntü sınırları içinde kalacak şekilde hesaplanır.
- Kırpma alanı oluşturulduktan sonra oran değiştirilirse mevcut alanın merkezi korunarak yeni orana oturtulur (senkron açıksa tüm görsellere uygulanır).

## Detay Görünümü ve Zoom

- Bir görsele **çift tıklayarak** detay görünümü **ayrı bir pencerede** açılır.
- Pencere açıldığında görsel **tam boyutunda (orijinal çözünürlük, en fazla %%4096 piksel genişliğe kadar)** gösterilir; büyük görsellerde kaydırma çubuğuyla başlangıca konumlanır.
- **Ctrl + Fare Tekerleği** ile imlecin bulunduğu nokta merkezli zoom (%%5 - %%800).
- **Sağ tuş ile sürükleyerek** kaydırma (pan).
- **Çift tık** ile zoom %%100'e sıfırlanır; **Sığdır** düğmesi görseli görünüme sığdırır.
- **Esc** veya **← Geri** düğmesi pencereyi kapatır; ana pencereye döner.
- Detay penceresi, görselin **kendi sabit önizleme alanını** kullanır ve ana penceredeki önizleme boyutuna **dokunmaz**; detayda zoom/sığdırma yapılırken ana penceredeki görüntü hiçbir şekilde değişmez.
- Kırpma koordinatları normalize saklandığı için detay penceresinde yapılan kırpmalar ana pencerede de aynı konumda kalır; iki görünüm arasında geçişte hiçbir kırpma bozulmaz.

## Görünüm Düzeni

- Görseller **çok sütunlu ızgara düzeninde** (WrapPanel) listelenir; sütun sayısı **kaydırıcı olmadan**, pencere genişliğine göre otomatik belirlenir.
- Az sayıda görsel yüklendiğinde önizleme, araç çubuğundaki **"Önizleme" kaydırıcısı ile belirlenen orana göre** (varsayılan: orijinal boyutun %%25'i) boyutlanır; kaydırıcı değiştirildikçe önizlemeler anında güncellenir. Çok sayıda görselde pencereye sığdırılır.
- Görseller pencere boyutuna göre en-boy oranı korunarak otomatik ölçeklenir; kırpma alanları boyut değişse de korunur (koordinatlar normalize saklanır).
- Çalışılan görsel kırmızı çerçeveyle vurgulanır.

## Dışa Aktarma

- Dışa aktarma **asenkron** çalışır; durum çubuğundaki ilerleme çubuğu ile takip edilebilir ve **İptal** edilebilir.
- **Ayarlar** penceresinden:
  - Çıktı formatı: kaynak format, JPG, PNG, BMP veya GIF
  - JPG kalitesi (50-100)
  - Son ek (varsayılan: `_kirpilmis`)
  - Hedef klasör (varsayılan: kaynak klasörü)
- Aynı ad mevcutsa üzerine yazılmaz; dosyaya `(1)`, `(2)` şeklinde eklenir.

## Değişiklik Günlüğü

### v1.4.9

- **Uygulama ikonu `bic.ico` olarak güncellendi:** `.exe` dosyasının ikonu (`ApplicationIcon`) ve ana pencere, Detay penceresi ve Ayarlar penceresinin başlık çubuğu ikonları `bic.ico`'yu kullanır (çok boyutlu, Windows uyumlu).

### v1.4.8

- **Önizleme boyutu kaydırıcısı:** Araç çubuğuna **"Önizleme"** kaydırıcısı eklendi (%10 - %100, varsayılan %25). Az sayıda görselde önizleme artık sabit %25 yerine, kaydırıcıdaki oranda (orijinal boyutun yüzdesi) gösterilir; kaydırıcı değiştirildikçe önizlemeler anında yeniden boyutlanır. İlk açılışta varsayılan %25 olduğu için ilk önizleme yine orijinal boyutun %25'i olur.

### v1.4.7

- **Üst menü yatay dizilime döndü:** v1.4.6'da bölücü çizgileri kaldırmak için ToolBar şablonu ezilmişti; bu, varsayılan temayı yok ederek menü öğelerinin yatay dizilimini bozuyordu. Şablon müdahalesi tamamen geri alındı. Üst menü artık **tek bir yatay ToolBar** olarak yeniden yazıldı: tüm düğmeler ve seçenekler yatay sırada, gruplar arasında yalnızca ince ayraçlar, **araç çubuğu bant çizgileri (grip/bölücü) olmadan**.
- "Hakkında" ile "Meta Veriyi Koru" arasında da (istenen üzere) ayraç yok; tüm öğeler taşma menüsüne düşmeden (`OverflowMode="Never"`) aynı satırda kalır.

### v1.4.6

- **Senkron baştan yazıldı (bozulma düzeltildi):** v1.4.5'te kırpma alanı orijinal piksel uzayında hesaplanıp, ekrandaki görüntü boyutuna göre normalize eden setter'lara veriliyordu; önizlemeler ölçekliyken (örn. orijinalin %%25'i) kırpma alanı orantısız büyüyüp görüntüden taşıyordu. Senkron artık tüm matematikte **normalize (0-1) koordinatları** kullanıyor; önizleme/grid/detay ölçeği ne olursa olsun sonuç her zaman aynı.
  - **Serbest kırpma:** kaynağın normalize çerçevesi (konum + boyut oranı) birebir kopyalanır — her fotoğrafta aynı göreli bölge seçilir.
  - **En-boy oranı kilitli:** kırpmanın piksel oranı korunur; alan, kaynağın oransal merkez konumunda hedef görsele sığacak şekilde yeniden ölçeklenir. Farklı en-boy oranına sahip dosyalarda kırpma şekli (orn. 16:9) bozulmadan uygulanır.
- **Araç çubuğu bölücü çizgileri kaldırıldı:** "Hakkında" ile "Meta Veriyi Koru" arasındaki bölücü (ToolBar gripi/bant çizgisi) kaldırıldı. **Not:** Bu, ToolBar şablonunu ezdiği için menünün yatay dizilimini bozuyordu; **v1.4.7'de tek satır yatay ToolBar ile çözüldü (yukarıya bakın).**

### v1.4.5

- **Kolon seçimi kaldırıldı:** Araç çubuğundaki kolon kaydırıcısı ve "pencereyi kolon sayısına göre boyutlandır" davranışı kaldırıldı. Görseller artık **yalnızca pencere genişliğine göre otomatik sıralanıyor** (her görsel en fazla 320 px; pencere daraldıkça kendileri daralır, WrapPanel sütunları kendiliğinden sarar).
- **Orijinali Sil:** "Seçimi Temizle" düğmesinin soluna eklendi. İşaretliyken başarılı kırpma işleminden sonra kaynak (orijinal) dosyalar **geri dönüşüm kutusuna** atılır (`RecycleOption.SendToRecycleBin`) ve listeden kaldırılır; başarısız olanlar için hata gösterilir. Sonuç penceresinde silinen orijinal sayısı belirtilir.
- **Senkron, farklı en-boy oranlı dosyalarda doğru boyutlanıyor:** Senkron, kaynak kırpmasının şeklini koruyarak hedef görsele uyarlamaya çalıştı; ancak koordinat uzayı karışıklığı nedeniyle çoğu senaryoda bozuktu ve bu sürümde kullanıcılarda "senkron bozuldu" sorununa yol açtı. **v1.4.6'da senkron baştan yazılarak düzeltildi (yukarıya bakın).**

### v1.4.4

- **Detay penceresi tam boyutta açılır:** Görsel artık açılışta orijinal çözünürlüğünde (en fazla %%4096 piksel genişlik sınırıyla) gösterilir.
- **Ana pencere önizlemesi detaydan etkilenmez:** Detay penceresi artık kendi sabit taban boyutunda çalışıyor (kendi zoom alanı), paylaşılan `ImageItem`'in `DisplayWidth`/`DisplayHeight` değerini hiç değiştirmiyor. Detayda zoom/sığdırma yapıldığında ana penceredeki önizlemelerin boyutu sabit kalır.
- Kırpma türü/zoom aralığı bu mimariye uygun hale getirildi (%%5 - %%800).

### v1.4.3

- **Açılış çökmesi düzeltildi:** Dil radyo düğmesinin `IsChecked=True` durumu XAML yüklenirken erken ateşleniyor ve `StatusBar` henüz oluşmadığı için `NullReferenceException` atıp uygulamanın başlamasını engelliyordu. `UpdateLanguage()` içindeki `StatusText` erişimine null koruması eklendi; uygulama artık dil değişiminde/başlangıçta çökmeden açılır.

### v1.4.2

- **Detay görünümü ayrı pencereye taşındı** (`DetailWindow`): Ana penceredeki yer paylaşımlı (overlay) görünüm kaldırıldı; çift tıklayınca görsel bağımsız bir pencerede açılır (ana pencerenin ortasında, `CenterOwner`). İki pencere aynı `ImageItem`'i paylaştığı için detayda yapılan kırpma ana pencerede **canlı** olarak güncellenir; pencere kapanınca ızgara sığdırması ana pencereye geri yüklenir.
- **Detay penceresi özellikleri:** Ctrl + Tekerlek ile imleç merkezli zoom (%10 - %800), sağ tuş-sürükle ile pan, çift tık ile zoom sıfırlama, **Sığdır** düğmesi, **Esc** / **← Geri** ile kapatma, sol-sürükle ile kırpma (açık Oran ayarına uyar).
- **Araç çubuğu yeniden yapılandırıldı:** Tüm denetimler artık `ToolBarTray` içinde üç gruba ayrıldı ve her öğeye `OverflowMode="Never"` eklendi. Böylece dar pencerelerde bile hiçbir düğme "»" taşma menüsüne düşüp kaybolmuyor.
- **Dil değişimi sağlamlaştırıldı:** Kırpma sürüklendiği sırada dil değiştirilirse durum çubuğu artık ezilmiyor; pencere başlığı TR/EN'ye göre değişiyor; açık detay penceresi de ana dille senkron güncelleniyor. TR/EN radyo butonlarına `GroupName` eklendi (ayrı araç çubuklarında da tek seçim korunur).
- **Tek görsel önizlemesi varsayılan %%25:** Yüklenen görsel sayısı kolon sayısından az olduğunda önizleme, orijinal boyutun **%%25'i** kadar büyük gösterilir (en az bir kolon genişliği, en fazla görünüm genişliği sınırıyla). Çok sayıda görsel yüklendiğinde ızgara sığdırması uygulanır.
- **Kaydırma çubuğu ile pencere arası boşluk giderildi:** `WrapPanel` genişliği doğrudan `ScrollViewer.ViewportWidth` değerine bağlandı; içerik artık görünümü tam boyda kaplar, sağda boş kalan sütun kalmaz.

### v1.4.1

- Pencere genişliği **kolon sayısına göre otomatik boyutlanır** (kolon × 300; 640-1920 px ve ekran genişliği sınırları; tam ekran durumunda atlanır).
- **Kararlı yeniden yerleşim:** Görseller pencereye sığmadığında oluşan sürekli yeniden yükleme/yerleşim döngüsü düzeltildi (`ViewportWidth` tabanlı hesaplama, yeniden giriş koruması, `Dispatcher` ile birleştirme; boyutu değişmeyen görseller atlanır).
- **Kaydırma çubuğu ile pencere arası boşluk giderildi:** `WrapPanel` dış marjı kaldırıldı, içerik hizalaması yatayda `Stretch` / dikeyde `Top` yapıldı.
- **Boş bırakma alanı filigranı** pencere yüksekliğine göre ölçeklenir (yükseklik × %%10, 18-44 px sınır; dar pencerede alt satıra taşabilir).
- Dil desteği ve detay görünümüyle ilgili arayüz metinleri güncellendi.

### v1.4.0

- **Detay görünümü ve zoom:** Ctrl + Fare Tekerleği ile imleç merkezli zoom (%10 - %800), sağ tuş-sürükle ile pan, çift tık ile sıfırlama, **Sığdır** düğmesi, **Esc** / **Geri** ile çıkış.
- **En-boy oranı:** Serbest, 1:1, 4:3, 3:2, 16:9, 5:4, 21:9, 3:4, 2:3, 9:16. Seçili oranda çizilen alan orana **birebir uyar**; mevcut alanın oranı değiştirilince merkez korunur (senkron açıksa tüm görsellere uygulanır).
- **Çoklu görsel yükleme kararlılığı:** `ImageItem` ve `BitmapImage` UI iş parçacığında oluşturulur, yalnızca ağır görüntü çözme arka planda (`Task.Run`) çalışır; çok sayıda fotoğraf yüklenirken arayüz donmaz ve çökme yaşanmaz.
- **Dışa aktarma ayarları:** Çıktı formatı (kaynak/JPG/PNG/BMP/GIF), JPG kalitesi, son ek ve hedef klasör seçilebilir ayarlar penceresinden yönetilir.
- **Çok sütunlu ızgara düzeni:** Kolon sayısı kaydırıcısıyla (1-6) ayarlanır, görseller pencere boyutuna göre en-boy oranı korunarak ölçeklenir.
- **Asenkron dışa aktarma:** İlerleme çubuğu ve **İptal** düğmesi; aynı adda dosya varsa `(1)`, `(2)` şeklinde benzersiz ad üretilir.

### v1.3.0 (önceki)

- Girdi alanı çizme/taşıma, `Delete` tuşu ile görsel kaldırma, çalışılan görselin kırmızı çerçeveyle vurgulanması, PNG/JPG uzantı eşleşme hatası düzeltmesi, temizlik ve küçük iyileştirmeler.

## Gereksinimler

- .NET 8 sürümünü gerektirir.