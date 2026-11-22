namespace FitTeam.Core;
public sealed record ExerciseGuide(string Movement,string Instructions,string? HomeAlternative=null);
public static class ExerciseGuides
{
    private static readonly Dictionary<string,ExerciseGuide> guides=new()
    {
        ["Duvar / diz üstü şınav"]=new("pushup","Ellerini omuz genişliğinde yerleştir. Dizlerin yerde veya ellerin duvarda olsun. Dirseklerini bükerek kontrollü yaklaş ve başlangıca dön.","Masa destekli şınav"),
        ["Masa destekli şınav"]=new("tablepushup","Yük taşıyabilen, kaymayan sağlam bir masaya ellerini yerleştir. Gövdeni düz tutarak dirseklerini kontrollü bük ve başlangıca dön. Cam veya katlanır masa kullanma."),
        ["Duvar şınavı"]=new("wallpushup","Ellerini omuz genişliğinde sağlam duvara yerleştir. Ayakların yerde, gövden düz olsun. Dirseklerini bükerek duvara yaklaş ve geri it."),
        ["Yatarak halter itişi"]=new("barbellbench","Sehpa üzerinde sırtüstü yat, ayaklarını yere bas. Ağırlığı kontrollü indir ve kollarını yukarı uzat.","Masa destekli şınav"),
        ["Dambılla göğüs açış"]=new("fly","Sırtüstü yat, dirseklerini hafif bük. Kollarını kontrollü yana aç ve göğüs üzerinde birleştir.","Masa destekli şınav"),
        ["Dambılla sırt çekişi"]=new("row","Gövdeni dengeli tut. Dirseğini gövdene yakın geriye çek, ağırlığı kontrollü indir."),
        ["Yukarıdan sırt çekişi"]=new("pulldown","Otur, gövdeni dengeli tut. Tutacağı göğüs üstüne doğru indir ve kontrollü geri bırak.","Dört ayak üzerinde çapraz uzanma"),
        ["Oturarak sırt çekişi"]=new("seatedrow","Dik otur. Dirseklerini geriye çekerek tutacağı gövdene yaklaştır, kontrollü geri uzan.","Dört ayak üzerinde çapraz uzanma"),
        ["Sandalyeye çömelme"]=new("squat","Ayaklarını dengeli aç. Kalçanı geriye vererek sağlam sandalyeye doğru alçal ve tekrar doğrul."),
        ["Dambıl tutarak çömelme"]=new("squat","Ağırlığı göğüs önünde tut. Kalçanı geriye vererek kontrollü alçal ve doğrul.","Sandalyeye çömelme"),
        ["Makinede bacak itişi"]=new("legpress","Sırtını desteğe yasla. Dizlerini kontrollü açarak platformu it; dizlerini kilitlemeden geri dön.","Sandalyeye çömelme"),
        ["Kalça köprüsü"]=new("bridge","Sırtüstü yat, dizlerini bük, ayaklarını yere bas. Kalçanı yukarı kaldırıp kontrollü indir."),
        ["Parmak ucuna yükselme"]=new("calf","Dengeni korumak için sağlam bir destek kullan. Topuklarını kaldırarak parmak ucuna yüksel ve kontrollü indir."),
        ["Dambılla omuz itişi"]=new("press","Dambılları omuz hizasında tut. Gövdeni sabit tutarak yukarı it ve kontrollü omuz hizasına dön.","Duvar şınavı"),
        ["Yana omuz açış"]=new("raise","Dirsekleri hafif bük. Kollarını kontrollü omuz hizasına kadar yana kaldırıp indir.","Hafif üst vücut hareketliliği"),
        ["Dambılla kol bükme"]=new("curl","Dirseklerini gövdene yakın tut. Ağırlığı omuz yönüne kaldır ve kontrollü indir."),
        ["Makarada arka kol itişi"]=new("triceps","Dirseklerini gövdene yakın sabitle. Kollarını aşağı doğru uzat ve kontrollü geri dön.","Masa destekli şınav"),
        ["Dirsek üzerinde sabit duruş"]=new("plank","Dirseklerini omuz altında tut. Gövdeni düz bir çizgide sabit tut ve nefesini düzenli sürdür."),
        ["Dört ayak üzerinde çapraz uzanma"]=new("birddog","Eller ve dizler yerde başla. Bir kolunu ve karşı bacağını uzat; dengeli biçimde geri dön ve taraf değiştir."),
        ["Sırtüstü çapraz kol ve bacak uzatma"]=new("deadbug","Sırtüstü yat, kolları yukarı ve dizleri bükülü tut. Karşı kol ve bacağı kontrollü uzatıp başlangıca dön."),
        ["Tempolu yürüyüş"]=new("walk","Dengeli adımlarla yürü. Kollarını doğal biçimde hareket ettir ve programındaki süreyi takip et."),
        ["Sabit bisiklet"]=new("cycle","Sele yüksekliğini uygun ayarla. Dengeli otur, pedalı kontrollü çevir.","Tempolu yürüyüş"),
        ["Dinamik ısınma"]=new("warmup","Hafif adımlarla başla. Kollarını ve bacaklarını kontrollü hareket ettirerek ısın."),
        ["Hafif üst vücut hareketliliği"]=new("mobility","Omuzları ve kolları kontrollü hareket ettir. Hareket aralığını zorlamadan uygula."),
        ["Top sürme ve pas"]=new("football","Topu yakınında kontrollü sür. Dengeni koruyarak kısa pas ver; uygun boş alanda çalış."),
        ["Reaksiyon ve ayak çalışması"]=new("run","Dengeli kısa adımlarla yön değiştir. İşaret veya komuta göre kontrollü hareket et."),
        ["Aralıklı koşu"]=new("run","Programındaki çalışma ve dinlenme aralıklarını takip et. Uygun ve güvenli alanda koş."),
        ["Çeviklik parkuru"]=new("run","İşaretler arasında dengeli adımlarla ilerle. Yön değiştirirken kontrollü hareket et."),
        ["Top sürme ve pas çalışması"]=new("basketball","Topu kontrollü sektir. Dengeli duruştan göğüs pası çalış."),
        ["Pas ve servis çalışması"]=new("volleyball","Dengeli dur. Topu kontrollü yönlendir; servis hareketini programındaki teknikle uygula.")
    };
    public static ExerciseGuide? For(string name,bool home=false)
    {
        if(!guides.TryGetValue(name,out var guide))return null;
        return home&&guide.HomeAlternative!=null?guides[guide.HomeAlternative]:guide;
    }
    public static string PerformedName(string name,bool home)=>home&&guides.TryGetValue(name,out var guide)&&guide.HomeAlternative!=null?guide.HomeAlternative:name;
}
