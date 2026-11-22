using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using FitTeam.Core;
using System.Globalization;
namespace FitTeam.Desktop.Views;
public partial class MainWindow
{
    private static Control Columns(Control left,Control right,double leftWidth=320)
    {
        var g=new Grid{ColumnDefinitions=new ColumnDefinitions($"{leftWidth},*"),ColumnSpacing=22};g.Children.Add(left);Grid.SetColumn(right,1);g.Children.Add(right);return g;
    }
    private static Control Empty(string title,string text,string symbol="◇")=>Ui.Stack(Ui.Text(symbol,38,Ui.Muted,true),Ui.Text(title,21,Ui.Ink,true),Ui.Text(text,14,Ui.Muted));
    private static TextBox Notes(string value="",string hint="Not") {var b=Ui.Input(value,hint);b.AcceptsReturn=true;b.MinHeight=95;b.MaxLength=2000;return b;}
    private static Control PortalStat(string label,string value,string symbol){var grid=new Grid{ColumnDefinitions=new("44,*"),ColumnSpacing=12};grid.Children.Add(new Border{Background=Brush.Parse("#F4FCD8"),CornerRadius=new(12),Padding=new(9),VerticalAlignment=VerticalAlignment.Center,Child=Ui.Text(symbol,20)});var text=Ui.Stack(Ui.Text(value,26,Ui.Ink,true),Ui.Text(label,12,Ui.Muted));Grid.SetColumn(text,1);grid.Children.Add(text);var card=Ui.Card(grid);card.Padding=new(16);card.MinHeight=112;return card;}
    private Control PortalDashboard()
    {
        var today=DateOnly.FromDateTime(DateTime.Today);var plan=service.Plan(Me);var logs=service.Logs(Me);var diary=service.Diary(Me);var coaches=service.SelectedCoaches(Me);var day=plan?.Days.FirstOrDefault(d=>d.Date==today);
        var greeting=Ui.Card(Ui.Stack(Ui.UserText(AppLanguage.Text("Merhaba")+", "+Me.Profile.Name.Split(' ')[0]+" 👋",27,Brushes.White,true),Ui.Text(coaches.Count==0?"Henüz bir antrenörün yok. Sana uygun antrenörü seçerek başlayabilirsin.":"Antrenmanın, kayıtların ve gelişimin tek yerde.",15,Brush.Parse("#B5BAC0")),Ui.Row(Ui.Button(coaches.Count==0?"Antrenör bul":"Antrenmanımı aç",()=>ShowPage(coaches.Count==0?"Antrenörüm":"Antrenmanım"),true))));greeting.Background=Brush.Parse("#14181D");greeting.Padding=new(30);
        var todayPanel=day==null||day.Exercises.Count==0?Empty(day==null?"Henüz programın yok":"Bugün dinlenme günü",day==null?"Programlarım bölümünden bir program oluşturabilirsin.":"Dinlen, günlük kayıtlarını ve su hedefini takip et.","⚡"):Ui.Stack(Ui.UserText(day.Title,22,Ui.Ink,true),Ui.Text(day.Status,13,Ui.Green),Ui.Stack(day.Exercises.Select(e=>Ui.Text($"{e.Name} · {e.Sets} × {e.Reps} · {e.RestSeconds} sn",14)).ToArray()),Ui.AsyncButton("Antrenmana başla",()=>Guard(()=>{OpenSession(day!.Id);return Task.CompletedTask;}),true));
        var recent=new StackPanel{Spacing=12};foreach(var e in diary.Take(4))recent.Children.Add(Ui.Stack(Ui.Text(e.Date.ToString("dd MMM yyyy"),12,Ui.Muted),Ui.UserText($"{AppLanguage.Text(e.Kind)} · {e.Value:0.##} {DiaryUnit(e.Kind)} · {e.Title}"),Ui.UserText(e.Note,13,Ui.Muted)));
        if(recent.Children.Count==0)recent.Children.Add(Empty("Henüz kayıt yok","Su, uyku, adım ve antrenman kayıtlarını günlüğüne ekle.","▤"));
        var week=WeekActivityCard();var coachCard=Ui.Card(Ui.Stack(Ui.Text("Antrenörüm",19,Ui.Ink,true),Ui.UserText(coaches.Count==0?AppLanguage.Text("Henüz antrenör seçmedin."):string.Join("\n",coaches.Select(c=>c.Name))),Ui.Button("Antrenörleri incele",()=>ShowPage("Antrenörüm"))));
        var details=new Grid{ColumnDefinitions=new("*,*"),ColumnSpacing=22};details.Children.Add(Ui.Card(Ui.Stack(Ui.Text("⚡ Bugün",20,Ui.Ink,true),todayPanel)));var side=Ui.Stack(coachCard,week);Grid.SetColumn(side,1);details.Children.Add(side);
        return Ui.Page(service.IsPreview?Ui.Card(Ui.Text("Örnek üye paneli • Bu ekrandaki kayıtlar denemedir; gerçek hesaplara yazılmaz.",13,Ui.Green)):new Border{IsVisible=false},greeting,Ui.Stats(PortalStat("Aktif program",plan==null?"0":"1","▤"),PortalStat("Son 30 gün antrenman",logs.Count(l=>l.Date>=today.AddDays(-29)&&l.Completed).ToString(),"↗"),PortalStat("Bu hafta kayıt",diary.Count(d=>d.Date>=today.AddDays(-6)).ToString(),"▦"),PortalStat("Ölçüm",service.Measurements(Me).Select(m=>m.Date).Union(service.Weights(Me).Select(w=>w.Date)).Count().ToString(),"◎")),details,Ui.Card(Ui.Stack(Ui.Row(Ui.Text("Son kayıtlarım",20,Ui.Ink,true),Ui.Button("Tümü",()=>ShowPage("Günlüğüm"))),recent)));
    }
    private Control WeekActivityCard()
    {
        var today=DateOnly.FromDateTime(DateTime.Today);var diary=service.Diary(Me);var logs=service.Logs(Me);var bars=new Grid{ColumnDefinitions=new("*,*,*,*,*,*,*"),ColumnSpacing=8};
        for(int i=0;i<7;i++){var date=today.AddDays(i-6);var count=diary.Count(d=>d.Date==date)+logs.Count(l=>l.Date==date);var p=Ui.Stack(new Border{Height=6+Math.Min(count,8)*10,Background=count>0?Ui.Green:Brush.Parse("#E3E7EA"),CornerRadius=new(4),VerticalAlignment=VerticalAlignment.Bottom},Ui.Text(CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedDayName(date.DayOfWeek),11,Ui.Muted));p.VerticalAlignment=VerticalAlignment.Bottom;Grid.SetColumn(p,i);bars.Children.Add(p);}bars.Height=100;
        return Ui.Card(Ui.Stack(Ui.Row(Ui.Text("Son 7 gün",18,Ui.Ink,true),Ui.Button("Günlüğüm",()=>ShowPage("Günlüğüm"))),bars));
    }
    private static string DiaryUnit(string kind)=>AppLanguage.Text(kind switch{"Su"=>"litre","Uyku"=>"saat","Adım"=>"adım","Antrenman"=>"dk","Öğün"=>"kcal",_=>""});
    private Control DiaryPage()
    {
        var kind=Ui.Combo(FitService.DiaryKinds);var date=Ui.Date(DateOnly.FromDateTime(DateTime.Today),true);var value=Ui.Number(45,0,100000,1);var title=Ui.Input("","Başlık");var note=Notes("","Nasıl geçti?");var valueLabel=Ui.Text("Değer (dk)",13,Ui.Muted);
        kind.SelectionChanged+=(_,_)=>{valueLabel.Text=AppLanguage.Text("Değer")+" ("+DiaryUnit(Ui.Selected(kind))+")";value.Value=Ui.Selected(kind) switch{"Su"=>0.25m,"Uyku"=>8,"Adım"=>5000,"Öğün"=>500,"Not"=>0,_=>45};value.IsEnabled=Ui.Selected(kind)!="Not";};
        var add=Ui.Card(Ui.Stack(Ui.Text("＋ Kayıt ekle",20,Ui.Ink,true),Ui.Text("Kayıtlarını bağlı olduğun antrenörlerin de görebilir.",13,Ui.Muted),Ui.Field("Tür",kind,260),Ui.Field("Tarih",date,260),valueLabel,value,Ui.Field("Başlık",title,260),note,Ui.AsyncButton("Kaydet",()=>Run(()=>{service.AddDiary(Ui.DateValue(date),Ui.Selected(kind),Ui.Value(value),title.Text??"",note.Text??"");Refresh();}),true)));
        var entries=new StackPanel{Spacing=12};foreach(var d in service.Diary(Me))entries.Children.Add(Ui.Card(Ui.Stack(Ui.Row(Ui.Text(d.Date.ToString("dd MMM yyyy"),12,Ui.Muted),Ui.Text(d.Kind,12,Ui.Green)),Ui.UserText($"{d.Title} · {d.Value:0.##} {DiaryUnit(d.Kind)}",18,Ui.Ink,true),Ui.UserText(d.Note,14,Ui.Muted))));
        if(entries.Children.Count==0)entries.Children.Add(Ui.Card(Empty("Henüz kayıt yok","Su, uyku, adım, antrenman ve öğünlerini buradan ekle.","▦")));
        return Ui.Page(Columns(add,Ui.Stack(WeekActivityCard(),Ui.Text("Günlüğüm",20,Ui.Ink,true),entries)));
    }
    private decimal? OptionalNumber(TextBox input)
    {
        if(string.IsNullOrWhiteSpace(input.Text))return null;var text=LoginContact.NormalizeDigits(input.Text.Trim()).Replace('٫',',').Replace('.',',');
        if(!decimal.TryParse(text,NumberStyles.AllowDecimalPoint|NumberStyles.AllowLeadingSign,CultureInfo.GetCultureInfo("tr-TR"),out var v))throw new ArgumentException("Ölçüm alanlarına sayı gir.");return v;
    }
    private sealed record MeasurementRow(string Date,string Weight,string Fat,string Muscle,string Chest,string Waist,string Hip,string Arm,string Leg);
    private Control MeasurementsPage()
    {
        var date=Ui.Date(DateOnly.FromDateTime(DateTime.Today),true);var inputs=Enumerable.Range(0,8).Select(_=>Ui.Input("","—")).ToArray();var labels=new[]{"Kilo (kg)","Yağ oranı (%)","Kas (kg)","Göğüs (cm)","Bel (cm)","Kalça (cm)","Kol (cm)","Bacak (cm)"};var fields=new WrapPanel();for(int i=0;i<8;i++)fields.Children.Add(Ui.Field(labels[i],inputs[i],120));var note=Notes();
        string F(decimal? v)=>v.HasValue?v.Value.ToString("0.##"):"—";
        var rows=service.Measurements(Me).Select(m=>new MeasurementRow(m.Date.ToString("dd MMM yyyy"),F(m.Weight),F(m.FatPercent),F(m.MuscleKg),F(m.Chest),F(m.Waist),F(m.Hip),F(m.Arm),F(m.Leg))).ToList();
        foreach(var w in service.Weights(Me).Where(w=>!service.Measurements(Me).Any(m=>m.Date==w.Date)))rows.Add(new(w.Date.ToString("dd MMM yyyy"),F(w.Weight),"—","—","—","—","—","—","—"));
        var table=Ui.Table(rows.OrderByDescending(r=>DateTime.Parse(r.Date,CultureInfo.CurrentCulture)),("Tarih","Date"),("Kilo","Weight"),("Yağ %","Fat"),("Kas","Muscle"),("Göğüs","Chest"),("Bel","Waist"),("Kalça","Hip"),("Kol","Arm"),("Bacak","Leg"));table.MinWidth=750;var scroller=new ScrollViewer{Content=table,HorizontalScrollBarVisibility=Avalonia.Controls.Primitives.ScrollBarVisibility.Auto};
        var add=Ui.Card(Ui.Stack(Ui.Text("＋ Ölçüm ekle",20,Ui.Ink,true),Ui.Text("Aynı günün ölçümü güncellenir. Bilmediğin alanları boş bırakabilirsin.",13,Ui.Muted),Ui.Field("Tarih",date,260),fields,note,Ui.AsyncButton("Ölçümü kaydet",()=>Run(()=>{var v=inputs.Select(OptionalNumber).ToArray();service.SaveMeasurement(new(){Date=Ui.DateValue(date),Weight=v[0],FatPercent=v[1],MuscleKg=v[2],Chest=v[3],Waist=v[4],Hip=v[5],Arm=v[6],Leg=v[7],Note=note.Text??""});Refresh();}),true)));
        return Ui.Page(Columns(add,Ui.Stack(Ui.Card(Ui.Stack(Ui.Text("Ölçüm geçmişim",20,Ui.Ink,true),rows.Count==0?Empty("Henüz ölçüm yok","İlk ölçümünü ekleyerek başlayabilirsin."):scroller)),Ui.Button("Gelişim grafiklerini aç",()=>ShowPage("Gelişim grafikleri")))));
    }
    private Control FoodGuidePage()
    {
        var search=Ui.Input("","Gıda ara");var category=Ui.Combo(new[]{"Tümü"}.Concat(FoodGuide.All.Select(f=>f.Category).Distinct()));var grams=Ui.Number(100,1,2000);var grid=new WrapPanel();var paging=Ui.Text("");int page=0;
        void Render(){grid.Children.Clear();var items=FoodGuide.All.Where(f=>(f.Name.Contains(search.Text??"",StringComparison.OrdinalIgnoreCase)||AppLanguage.Text(f.Name).Contains(search.Text??"",StringComparison.OrdinalIgnoreCase))&&(Ui.Selected(category)=="Tümü"||f.Category==Ui.Selected(category))).ToList();int max=Math.Max(0,(items.Count-1)/12);page=Math.Min(page,max);foreach(var f in items.Skip(page*12).Take(12)){var factor=Ui.Value(grams)/100;var card=Ui.Card(Ui.Stack(Ui.Text(f.Name,17,Ui.Ink,true),Ui.Text($"{Math.Round(f.Calories*factor)} kcal",23,Ui.Ink,true),Ui.Text($"{AppLanguage.Text("Protein")}: {f.Protein*factor:0.#} g\n{AppLanguage.Text("Karbonhidrat")}: {f.Carbs*factor:0.#} g\n{AppLanguage.Text("Yağ")}: {f.Fat*factor:0.#} g",12,Ui.Muted)));card.Width=220;card.Margin=new(0,0,14,14);grid.Children.Add(card);}paging.Text=$"{page+1} / {max+1} · {items.Count}";}
        search.TextChanged+=(_,_)=>{page=0;Render();};category.SelectionChanged+=(_,_)=>{page=0;Render();};grams.ValueChanged+=(_,_)=>Render();Render();
        return Ui.Page(Ui.Card(Ui.Stack(Ui.Text("Gıda rehberi",23,Ui.Ink,true),Ui.Text("Genel, yaklaşık besin değerleri. Porsiyonu gram olarak ayarla; ürün etiketini esas al.",13,Ui.Muted),Ui.Row(Ui.Field("Ara",search,280),Ui.Field("Kategori",category,240),Ui.Field("Porsiyon (g)",grams,150)),grid,Ui.Row(Ui.Button("Önceki",()=>{page=Math.Max(0,page-1);Render();}),paging,Ui.Button("Sonraki",()=>{page++;Render();})))));
    }
    private Control DietPage()
    {
        var meals=service.MealPlan(Me);var content=new StackPanel{Spacing=16};
        foreach(var meal in meals)content.Children.Add(Ui.Card(Ui.Stack(Ui.Text(meal.Meal,20,Ui.Ink,true),Ui.UserText(meal.Contents,17),Ui.UserText(meal.Note,13,Ui.Muted))));
        if(meals.Count>0)content.Children.Add(new Expander{Header=AppLanguage.Text("Hedefe göre alternatif öğünler"),Content=MealSuggestionCard(),IsExpanded=false});else content.Children.Add(MealSuggestionCard());
        return Ui.Page(content,Ui.Row(Ui.Button("Antrenörüme mesaj yaz",()=>ShowPage("Mesajlar"),true),Ui.Button("Kalori ve beslenme kaydım",()=>ShowPage("Kalori ve beslenme")),Ui.Button("Gıda rehberini aç",()=>ShowPage("Gıda Rehberi"))));
    }
    private Control RemindersPage()
    {
        var stored=service.Reminders();var controls=new List<(string Kind,CheckBox Enabled,TextBox Hour)>();
        Control Setting(string kind,int count){var panel=Ui.Stack(Ui.Text(kind=="Su"?"Su içme":"Spor",21,Ui.Ink,true),Ui.Text("Boş bıraktığın saat için hatırlatma gönderilmez.",13,Ui.Muted));var items=stored.Where(r=>r.Kind==kind).ToList();for(int i=0;i<count;i++){var r=items.ElementAtOrDefault(i);var cb=new CheckBox{IsChecked=r?.Enabled??false,Content=(i+1).ToString()};var time=Ui.Input(r?.Time.ToString("HH:mm")??"","09:00");time.Width=150;time.FlowDirection=FlowDirection.LeftToRight;controls.Add((kind,cb,time));panel.Children.Add(Ui.Row(cb,time));}return Ui.Card(panel);}
        return Ui.Page(Columns(Setting("Su",3),Setting("Antrenman",1),440),Ui.Card(Ui.Row(Ui.Text("Hatırlatmalar uygulama açıkken kontrol edilir.",13,Ui.Muted),Ui.AsyncButton("Kaydet",()=>Run(()=>{var list=new List<MemberReminder>();foreach(var c in controls){if(string.IsNullOrWhiteSpace(c.Hour.Text))continue;if(!TimeOnly.TryParseExact(LoginContact.NormalizeDigits(c.Hour.Text.Trim()),"HH:mm",CultureInfo.InvariantCulture,DateTimeStyles.None,out var time))throw new ArgumentException("Saati 09:00 biçiminde gir.");list.Add(new(){Kind=c.Kind,Time=time,Enabled=c.Enabled.IsChecked==true});}service.SaveReminders(list);Refresh();}),true))),Ui.Card(Ui.Stack(Ui.Text("Bildirimleri dene",20,Ui.Ink,true),Ui.Text("Bu Mac’te uygulama içindeki bildirim kutusuna bir deneme gönder.",14,Ui.Muted),Ui.Button("Deneme bildirimi gönder",()=>{service.TestNotification();ShowPage("Bildirimler");},true))),Ui.Card(Ui.Stack(Ui.Text("Nasıl çalışır?",20,Ui.Ink,true),Ui.Text("Aynı hatırlatma günde bir kez görünür. Saatini 15 dakikadan fazla kaçırırsan o gün atlanır. Uygulama kapalıyken veya telefona gönderim bu yerel sürümde bulunmaz.",14,Ui.Muted))));
    }
    private Control NotificationsPage()
    {
        var panel=new StackPanel{Spacing=14};foreach(var n in service.Notifications())panel.Children.Add(Ui.Card(Ui.Stack(Ui.Row(Ui.Text(n.Title,19,Ui.Ink,true),Ui.Text(n.ReadAt==null?"Yeni":"Okundu",12,Ui.Green)),Ui.UserText(n.Body),Ui.Text(n.CreatedAt.ToLocalTime().ToString("dd MMM yyyy HH:mm"),12,Ui.Muted),Ui.Row(Ui.Button("Aç",()=>{service.MarkNotification(n.Id);ShowPage(n.Page);}),Ui.Button("Okundu işaretle",()=>{service.MarkNotification(n.Id);Refresh();}),Ui.Button("Listeden kaldır",()=>{service.MarkNotification(n.Id,true);Refresh();})))));
        if(panel.Children.Count==0)panel.Children.Add(Ui.Card(Empty("Henüz bildirim yok","Mesajların, salon onayların ve hatırlatmaların burada görünür.","♧")));
        return Ui.Page(Ui.Row(Ui.Text($"{service.Notifications().Count} {AppLanguage.Text("bildirim")}",23,Ui.Ink,true),Ui.Button("Hepsini okundu işaretle",()=>{service.MarkNotification(null);Refresh();})),panel,Ui.Text("Okunan bildirimler 7 gün sonra listede gösterilmez. Okunmamış bildirimler korunur.",12,Ui.Muted));
    }
    private Guid? conversationId;
    private Control MessagesPage()
    {
        var contacts=service.MessageContacts();var list=new StackPanel{Spacing=8};var thread=new StackPanel{Spacing=12};
        foreach(var c in contacts){var b=Ui.Button(c.Profile.Name,()=>{conversationId=c.Id;ShowPage("Mesajlar");},conversationId==c.Id);b.Content=Ui.UserText(c.Profile.Name,14,conversationId==c.Id?Ui.Ink:Ui.Ink,true);list.Children.Add(b);}
        var contact=contacts.FirstOrDefault(c=>c.Id==conversationId)??contacts.FirstOrDefault();if(contact==null)return Ui.Page(Ui.Card(Empty("Henüz mesajlaşacağın kişi yok","Bir antrenör seçtikten sonra burada mesajlaşabilirsin.","☏")));
        conversationId=contact.Id;service.MarkConversationRead(contact.Id);
        foreach(var m in service.Conversation(contact.Id)){var card=Ui.Card(Ui.Stack(Ui.UserText(m.FromId==Me.Id?AppLanguage.Text("Ben"):contact.Profile.Name,12,Ui.Muted,true),Ui.UserText(m.Body),Ui.Text(m.SentAt.ToLocalTime().ToString("dd.MM HH:mm"),11,Ui.Muted)));card.Background=Brush.Parse(m.FromId==Me.Id?"#F2FACF":"#FFFFFF");thread.Children.Add(card);}
        if(thread.Children.Count==0)thread.Children.Add(Empty("Konuşmayı başlat","Mesajını aşağıya yazabilirsin.","☏"));
        var message=Notes("","Mesajını yaz");
        return Ui.Page(Columns(Ui.Card(Ui.Stack(Ui.Text("Kişiler",20,Ui.Ink,true),list)),Ui.Stack(Ui.UserText(contact.Profile.Name,23,Ui.Ink,true),thread,Ui.Card(Ui.Stack(message,Ui.AsyncButton("Gönder",()=>Run(()=>{service.SendMessage(contact.Id,message.Text??"");Refresh();}),true))),Ui.Text("Mesajlar bu bilgisayardaki FitTeam hesapları arasında saklanır.",12,Ui.Muted)),250));
    }
    private Control MyCoachesPage()
    {
        var search=Ui.Input("","Antrenör adı veya uzmanlık");var items=new StackPanel{Spacing=14};var checkedIds=FitService.CoachLinks(Me).ToHashSet();
        var available=service.Data.Users.Where(u=>u.IsActive&&!u.NeedsApproval&&u.Role==Choices.Roles[2]).ToList();
        var selectedCount=Ui.Text("",15,Ui.Green,true);
        void Count(){selectedCount.Text=AppLanguage.Text("Seçili hoca:")+" "+checkedIds.Count+" / "+available.Count;}
        void Render()
        {
            items.Children.Clear();
            var filtered=available.Where(u=>(u.Profile.Name+" "+u.CoachSpecialty).Contains(search.Text??"",StringComparison.OrdinalIgnoreCase)).ToList();
            foreach(var kind in new[]{"Kadın","Erkek","Diğer"})
            {
                var group=filtered.Where(u=>kind=="Kadın"?u.Profile.Gender is "Kadın" or "Kız":kind=="Erkek"?u.Profile.Gender=="Erkek":u.Profile.Gender is not ("Kadın" or "Kız" or "Erkek")).ToList();if(group.Count==0)continue;
                items.Children.Add(Ui.Text((kind=="Kadın"?"Kadın antrenörler":kind=="Erkek"?"Erkek antrenörler":"Diğer antrenörler")+" • "+group.Count,19,Ui.Ink,true));
                foreach(var c in group)
                {
                    var cb=new CheckBox{Content=c.Profile.Name,IsChecked=checkedIds.Contains(c.Id),Tag=c.Id,IsEnabled=Me.Role==Choices.Roles[0]};
                    cb.IsCheckedChanged+=(_,_)=>{if(cb.IsChecked==true)checkedIds.Add(c.Id);else checkedIds.Remove(c.Id);Count();};
                    items.Children.Add(Ui.Card(Ui.Stack(Ui.UserText(c.Profile.Name,22,Ui.Ink,true),Ui.UserText(c.CoachSpecialty,14,Ui.Muted),Ui.Text("Yaş: "+c.Profile.Age+" • Deneyim: "+c.ExperienceYears+" yıl"+(c.IsExample?" • "+AppLanguage.Text("Örnek hesap"):""),14,Ui.Muted),cb,Ui.Text(c.WorkHours.Count==0?"Çalışma saati belirtilmemiş.":string.Join(" · ",c.WorkHours.Select(h=>AppLanguage.Text(Choices.DayName(h.Day))+" "+h.Start.ToString("HH:mm")+"–"+h.End.ToString("HH:mm"))),13,Ui.Muted))));
                }
            }
            if(filtered.Count==0)items.Children.Add(Ui.Card(Empty("Antrenör bulunamadı","Yetkili antrenör eklediğinde burada seçebilirsin.","♙")));Count();LanguageView.Apply(items);
        }
        search.TextChanged+=(_,_)=>Render();Render();
        return Ui.Page(Ui.Card(Ui.Stack(Ui.Text("Antrenörüm",24,Ui.Ink,true),Ui.Text("Birden fazla antrenör seçebilirsin. Seçim sayısı için üst sınır yok; listede varsa on hoca da seçebilirsin.",14,Ui.Muted),selectedCount,search)),items,Me.Role==Choices.Roles[0]?Ui.AsyncButton("Seçtiğim antrenörleri kaydet",()=>Run(()=>{service.SelectCoaches(checkedIds);Refresh();}),true):Ui.Text("Takım antrenörün kulübüne göre belirlenir.",13,Ui.Muted));
    }
    private Control MyGymPage()
    {
        var search=Ui.Input("","Salon adı veya şehir");var city=Ui.Input("","Şehir");var panel=new StackPanel{Spacing=14};
        void Render(){panel.Children.Clear();foreach(var gym in service.GymDirectory().Where(t=>t.Name.Contains(search.Text??"",StringComparison.OrdinalIgnoreCase)&&t.City.Contains(city.Text??"",StringComparison.OrdinalIgnoreCase))){var membership=service.Data.GymMemberships.FirstOrDefault(m=>m.GymId==gym.Id&&m.UserId==Me.Id);panel.Children.Add(Ui.Card(Ui.Stack(Ui.UserText(gym.Name,22,Ui.Ink,true),Ui.UserText(gym.City,13,Ui.Muted),Ui.UserText(gym.Address,13,Ui.Muted),Ui.UserText(gym.Hours,13,Ui.Muted),Ui.UserText(gym.Description,14),Ui.Text(membership==null?"Üyelik başvurusu yapabilirsin.":membership.Approved?"Üyeliğin onaylandı.":membership.Rejected?"Başvurun onaylanmadı.":"Başvurun onay bekliyor.",14,Ui.Green),membership==null||membership.Rejected?Ui.AsyncButton("Başvur",()=>Run(()=>{service.RequestGym(gym.Id);Render();}),true):Ui.Button("Giriş ve çıkış kaydım",()=>ShowPage("QR Okut")))));}if(panel.Children.Count==0)panel.Children.Add(Ui.Card(Empty("Henüz salon yok","Yetkili salon eklediğinde burada bulabilirsin.","↗")));}search.TextChanged+=(_,_)=>Render();city.TextChanged+=(_,_)=>Render();Render();
        return Ui.Page(Ui.Card(Ui.Stack(Ui.Text("Spor Salonum",24,Ui.Ink,true),Ui.Row(Ui.Field("Salon ara",search,350),Ui.Field("Şehir",city,220)))),panel);
    }
    private Control QrPage()
    {
        var code=Ui.Input("","Örn: QYEUT8");code.FlowDirection=FlowDirection.LeftToRight;var visits=new StackPanel{Spacing=12};foreach(var v in service.Visits(Me).Take(30)){var gym=service.GymDirectory().FirstOrDefault(t=>t.Id==v.GymId);visits.Children.Add(Ui.Card(Ui.Stack(Ui.UserText(gym?.Name??"",18,Ui.Ink,true),Ui.Text("Giriş saati",12,Ui.Muted),Ui.Text(v.EnteredAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm")),Ui.Text("Çıkış saati",12,Ui.Muted),Ui.Text(v.LeftAt?.ToLocalTime().ToString("dd.MM.yyyy HH:mm")??AppLanguage.Text("İçeride")))));}if(visits.Children.Count==0)visits.Children.Add(Empty("Henüz kayıt yok","Salona giriş ve çıkışların burada görünür.","▦"));
        return Ui.Page(Columns(Ui.Card(Ui.Stack(Ui.Text("Salon QR’ını okut",22,Ui.Ink,true),Ui.Text("Salonun QR görselini seç veya altındaki 6 haneli kodu yaz. İlk okuma giriş, ikinci okuma çıkış olarak kaydedilir.",14,Ui.Muted),Ui.AsyncButton("Kamerayı aç",()=>Guard(async()=>{var value=await QrImages.Scan();if(value==null)return;service.CheckIn(value);Refresh();}),true),Ui.AsyncButton("QR görselinden oku",()=>Guard(async()=>{var files=await StorageProvider.OpenFilePickerAsync(new(){Title=AppLanguage.Text("QR görseli seç"),AllowMultiple=false,FileTypeFilter=[FilePickerFileTypes.ImageAll]});if(files.Count==0)return;var path=files[0].TryGetLocalPath()??throw new ArgumentException("Yerel bir QR görseli seç.");var value=await QrImages.Read(path);service.CheckIn(value);Refresh();}),true),Ui.Text("Kamera çalışmazsa QR görseli veya elle kod kullanabilirsin.",12,Ui.Muted),code,Ui.AsyncButton("Gönder",()=>Run(()=>{service.CheckIn(code.Text??"");Refresh();}),true))),Ui.Card(Ui.Stack(Ui.Text("Son hareketlerim",22,Ui.Ink,true),visits)),430));
    }
    private Control HealthNotesCard()
    {
        var note=Notes(Me.Profile.HealthNotes,"Antrenörünle paylaşmak istediğin bilgiler");note.MaxLength=3000;
        return Ui.Card(Ui.Stack(Ui.Text("Sağlık notlarım",21,Ui.Ink,true),Ui.Text("Bu notlar yalnız hesabının ve yetkili antrenörlerinin eriştiği yerel profilde saklanır.",13,Ui.Muted),note,Ui.AsyncButton("Notlarımı kaydet",()=>Run(()=>{service.SetHealthNotes(note.Text??"");Refresh();}),true)));
    }
    private Control PortalAdminPage()
    {
        var panel=new StackPanel{Spacing=16};panel.Children.Add(Ui.AsyncButton("＋ Spor salonu ekle",()=>Guard(()=>GymEditorDialog()),true));foreach(var gym in service.GymDirectory().Where(t=>service.HasAdministrationAccess||t.ManagerId==Me.Id)){
            var requests=new StackPanel{Spacing=8};foreach(var m in service.Data.GymMemberships.Where(m=>m.GymId==gym.Id&&!m.Approved&&!m.Rejected)){var member=service.Data.Users.FirstOrDefault(u=>u.Id==m.UserId&&u.IsActive);if(member!=null)requests.Children.Add(Ui.Row(Ui.UserText(member.Profile.Name),Ui.AsyncButton("Üyeliği onayla",()=>Run(()=>{service.ApproveGym(member.Id,gym.Id);Refresh();}),true),Ui.AsyncButton("Başvuruyu reddet",()=>Run(()=>{service.RejectGym(member.Id,gym.Id);Refresh();}))));}
            var code=service.GymCode(gym.Id);panel.Children.Add(Ui.Card(Ui.Stack(Ui.UserText(gym.Name,22,Ui.Ink,true),Ui.AsyncButton("Salonu düzenle",()=>Guard(()=>GymEditorDialog(gym))),Ui.Text("Salon giriş kodu",13,Ui.Muted),Ui.UserText(code,28,Ui.Ink,true),Ui.AsyncButton("QR görselini kaydet",()=>Guard(async()=>{var file=await StorageProvider.SaveFilePickerAsync(new(){Title=AppLanguage.Text("Salon QR görseli"),SuggestedFileName="FitTeam-QR.png",DefaultExtension="png"});if(file==null)return;var path=file.TryGetLocalPath()??throw new ArgumentException("Yerel bir dosya seç.");await QrImages.Create(code,path);}),true),Ui.Text("Bekleyen salon başvuruları",18,Ui.Ink,true),requests)));
        }
        var people=service.HasAdministrationAccess?service.Data.Users.Where(u=>u.IsActive&&!u.NeedsApproval&&Choices.Roles.Take(2).Contains(u.Role)).ToList():service.ManagedUsers();var selector=new ComboBox{ItemsSource=people,SelectedIndex=0,ItemTemplate=new Avalonia.Controls.Templates.FuncDataTemplate<User>((u,_)=>Ui.UserText(u?.Profile.Name??""))};var meal=Ui.Combo(Nutrition.Meals);var contents=Notes("","Öğündeki yiyecekler ve miktarlar");var note=Notes("","Üyeye not");
        panel.Children.Add(Ui.Card(Ui.Stack(Ui.Text("Üyeye beslenme listesi hazırla",23,Ui.Ink,true),selector,meal,contents,note,Ui.AsyncButton("Öğünü kaydet",()=>Run(()=>{var member=selector.SelectedItem as User??throw new ArgumentException("Üye seç.");service.AssignMeal(member,Ui.Selected(meal),contents.Text??"",note.Text??"");Refresh();}),true))));
        var memberSelector=new ComboBox{ItemsSource=people,SelectedIndex=0,ItemTemplate=new Avalonia.Controls.Templates.FuncDataTemplate<User>((u,_)=>Ui.UserText(u?.Profile.Name??""))};var memberDetails=new StackPanel{Spacing=10};
        void Detail(){memberDetails.Children.Clear();if(memberSelector.SelectedItem is not User member)return;foreach(var d in service.Diary(member).Take(10))memberDetails.Children.Add(Ui.UserText($"{d.Date:dd.MM} · {AppLanguage.Text(d.Kind)} · {d.Value} · {d.Title}\n{d.Note}"));memberDetails.Children.Add(Ui.UserText(member.Profile.HealthNotes));}
        memberSelector.SelectionChanged+=(_,_)=>Detail();Detail();panel.Children.Add(Ui.Card(Ui.Stack(Ui.Text("Üye günlükleri ve sağlık notları",22,Ui.Ink,true),memberSelector,memberDetails)));
        panel.Children.Add(CoachNutritionReview(people));
        return Ui.Page(panel);
    }
    private void OpenMemberPreview()
    {
        var previewService=new FitService(DataStore.Preview());previewService.Login("sporcu","Fit123!");
        var preview=new MainWindow(previewService);preview.ShowShell();preview.Title=AppLanguage.Text("FitTeam • Örnek üye paneli");preview.Show();
    }
    private void OpenAdministrationPreview()
    {
        var data=DataStore.Preview();AdditionalFitnessCoaches.Apply(data.Data);var previewService=new FitService(data);previewService.LoginPreviewAdministrator();
        var preview=new MainWindow(previewService);preview.ShowShell();preview.Title=AppLanguage.Text("FitTeam • Örnek yönetim paneli");preview.Show();
    }
}
