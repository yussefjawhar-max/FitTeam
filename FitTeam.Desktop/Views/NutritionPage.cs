using Avalonia.Controls;
using Avalonia.Controls.Templates;
using FitTeam.Core;
namespace FitTeam.Desktop.Views;
public partial class MainWindow
{
    private Control NutritionPage()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var activity = new ComboBox { ItemsSource = Nutrition.Activities, SelectedItem = Nutrition.Activities.FirstOrDefault(a => a.Id == Me.Profile.ActivityLevel),
            PlaceholderText = AppLanguage.Text("Hareket düzeyini seç"), ItemTemplate = new FuncDataTemplate<ActivityOption>((a, _) => Ui.Text(a?.Label ?? "")) };
        var special = new CheckBox { Content = "Besin alerjisi, hamilelik, emzirme veya özel beslenme takibi durumum var", IsChecked = Me.Profile.NutritionNeedsSpecialCare };
        var date = Ui.Date(today, true); var meal = Ui.Combo(Nutrition.Meals); var name = Ui.Input("", "Yiyecek veya içecek adı");
        var calories = Ui.Number(0, 0, 5000, 1); var portions = Ui.Number(1, 0.1m, 20, 1); var error = Ui.Text("", 13, Ui.Muted);
        var summary = new StackPanel { Spacing = 12 }; var foods = new StackPanel { Spacing = 8 };
        void Render()
        {
            summary.Children.Clear(); foods.Children.Clear();
            if (date.SelectedDate == null) { summary.Children.Add(Ui.Text("Yemek kayıtlarını görmek için gün seç.", 14, Ui.Muted)); return; }
            var estimate = Nutrition.Calculate(Me.Profile, (activity.SelectedItem as ActivityOption)?.Id ?? "", special.IsChecked == true);
            var chosenDate = Ui.DateValue(date); var entries = service.Foods(Me, chosenDate); decimal consumed = Nutrition.Consumed(entries);
            if (estimate.State == NutritionState.Ready)
            {
                summary.Children.Add(Ui.Stats(Ui.Stat("Kiloyu koruma kalorisi", estimate.Maintenance + " kcal"),
                    Ui.Stat("Günlük beslenme hedefi", estimate.Target + " kcal"), Ui.Stat("Kaydedilen yemekler", $"{consumed:0} kcal")));
                var remaining = Nutrition.Remaining(estimate, entries);
                summary.Children.Add(Ui.Text(remaining >= 0 ? $"Günlük hedefe kalan: {remaining:0} kcal" : $"Günlük hedefin üzerinde: {-remaining:0} kcal", 22, Ui.Green, true));
                summary.Children.Add(Ui.Text(estimate.Adjustment < 0 ? $"Koruma kalorisine göre yaklaşık {Math.Abs(estimate.Adjustment)} kcal daha az."
                    : estimate.Adjustment > 0 ? $"Koruma kalorisine göre yaklaşık {estimate.Adjustment} kcal daha fazla." : "Mevcut kilonu korumaya yönelik tahmini hedef.", 14, Ui.Muted));
                summary.Children.Add(Ui.Text("Öğünlere kalori dağılımı", 18, Ui.Ink, true));
                summary.Children.Add(Ui.Row(Nutrition.MealBudget(estimate.Target).Select(b => Ui.Stat(b.Meal, b.Calories + " kcal")).ToArray()));
                summary.Children.Add(Ui.Text("Öğün dağılımı isteğe göre değişebilir. Günlük hareket hesabı antrenmanı da kapsar; egzersiz kalorisi hedefe tekrar eklenmez.", 12, Ui.Muted));
            }
            else summary.Children.Add(Ui.Text(estimate.State switch
            {
                NutritionState.MissingActivity => "Günlük kalori hesabı için hareket düzeyini seç.",
                NutritionState.MissingProfile => "Kalori hesabı için Profilim sayfasındaki yaş, boy, kilo ve cinsiyet bilgilerini tamamla.",
                NutritionState.NeedsSpecialCare => "18 yaş altı veya özel beslenme durumunda otomatik kalori hedefi verilmez; kişisel plan için diyetisyen desteği gerekir.",
                _ => "Seçtiğin hedef kilo için otomatik kalori azaltımı yapılmıyor. Hedefini bir diyetisyenle değerlendir."
            }, 15, Ui.Ink, true));
            if (estimate.State != NutritionState.Ready) summary.Children.Add(Ui.Text($"Kaydedilen yemekler: {consumed:0} kcal", 20, Ui.Green, true));
            foreach (var entry in entries)
                foods.Children.Add(Ui.Row(Ui.UserText(entry.Name + " • " + AppLanguage.Text(entry.Meal) + " • " + entry.Portions.ToString("0.0") + " × " + entry.CaloriesPerPortion.ToString("0.0") + " = " + entry.Calories.ToString("0") + " kcal", 14),
                    Ui.Button("Kaydı kaldır", () => { try { service.RemoveFood(entry.Id); Render(); } catch (Exception ex) { error.Text = Ui.ErrorMessage(ex); } })));
            if (entries.Count == 0) foods.Children.Add(Ui.Text("Bu gün için henüz yemek kaydı yok.", 14, Ui.Muted));
            LanguageView.Apply(summary); LanguageView.Apply(foods);
        }
        activity.SelectionChanged += (_, _) => Render(); special.IsCheckedChanged += (_, _) => Render(); date.SelectedDateChanged += (_, _) => Render();
        Render();
        return Ui.Page(Ui.Card(Ui.Stack(Ui.Text("Kalori ve beslenme", 26, Ui.Ink, true), Ui.Text("Yaş, boy, kilo ve hareket düzeyinden yaklaşık günlük ihtiyaç hesaplanır. Kilo alma veya verme hedefine göre değişir.", 14, Ui.Muted),
            Ui.Field("Günlük hareket düzeyi", activity, 500), special,
            Ui.Button("Hareket düzeyimi kaydet", () => { try { service.SetNutritionSettings((activity.SelectedItem as ActivityOption)?.Id ?? "", special.IsChecked == true); error.Text = "Hareket düzeyin kaydedildi."; Render(); } catch (Exception ex) { error.Text = Ui.ErrorMessage(ex); } }),
            summary, Ui.Text("Bu değerler yetişkinler için yaklaşık hesaptır; kişisel beslenme planı yerine geçmez.", 12, Ui.Muted))),
            Ui.Card(Ui.Stack(Ui.Text("Günlük yemek önerilerim",21,Ui.Ink,true),Ui.Button("Hedefime göre öğünlerimi göster",()=>ShowPage("Beslenme Listem"),true))), WeightGoalCard(Me, true), Ui.Card(Ui.Stack(Ui.Text("Yediklerimi kaydet", 21, Ui.Ink, true),
                Ui.Text("Kaloriyi paketin etiketinden veya kullandığın besin kaynağından gir. Miktar ve porsiyona göre toplam hesaplanır.", 13, Ui.Muted),
                Ui.Row(Ui.Field("Gün", date), Ui.Field("Öğün", meal), Ui.Field("Yiyecek adı", name, 330)),
                Ui.Row(Ui.Field("Porsiyon başına kalori (kcal)", calories, 270), Ui.Field("Kaç porsiyon?", portions),
                    Ui.Button("Yemeği ekle", () => { try { service.AddFood(Ui.DateValue(date), Ui.Selected(meal), name.Text ?? "", Ui.Value(calories), Ui.Value(portions)); name.Text = ""; error.Text = "Yemek kaydedildi."; Render(); } catch (Exception ex) { error.Text = Ui.ErrorMessage(ex); } }, true)), error, foods)));
    }
}
