using Avalonia.Controls;
using Avalonia.Controls.Templates;
using FitTeam.Core;

namespace FitTeam.Desktop.Views;

public partial class MainWindow
{
    private async Task<bool> EditPersonalMeal(DateOnly date, SuggestedMeal meal, MealRoutine routine, string activity, bool specialCare)
    {
        bool saved = false;
        var title = Ui.Input(meal.Title);
        var choices = PersonalMealFoods.Available(routine);
        var rows = Ui.Stack();
        var totals = Ui.Text("", 15, Ui.Green, true);
        var error = Ui.Text("", 13, Ui.Muted);
        var fields = new List<(ComboBox Food, NumericUpDown Amount, ComboBox Unit, NumericUpDown GramsPerUnit)>();
        decimal Grams((ComboBox Food,NumericUpDown Amount,ComboBox Unit,NumericUpDown GramsPerUnit) row)
            =>Ui.Value(row.Amount)*(Ui.Selected(row.Unit)=="Gram"?1:Ui.Value(row.GramsPerUnit));
        void ShowTotals()
        {
            decimal calories = 0, protein = 0;
            foreach (var row in fields)
                if (row.Food.SelectedItem is FoodGuideItem food)
                {
                    calories += food.Calories * Grams(row) / 100;
                    protein += food.Protein * Grams(row) / 100;
                }
            calories += meal.ProteinProduct?.ProductCalories ?? 0;
            protein += meal.ProteinProduct?.ProductProtein ?? 0;
            totals.Text = AppLanguage.Text("Öğün toplamı:") + " " + Math.Round(calories) + " kcal • "
                + AppLanguage.Text("Protein") + ": " + protein.ToString("0.#") + " g";
        }
        void AddRow(FoodGuideItem food, decimal grams)
        {
            if (fields.Count >= 12) { error.Text = AppLanguage.Text("Bir öğüne en fazla 12 yiyecek ekleyebilirsin."); return; }
            var selector = new ComboBox { ItemsSource = choices, SelectedItem = food,
                ItemTemplate = new FuncDataTemplate<FoodGuideItem>((item, _) => Ui.Text(item?.Name ?? "")) };
            var amount = Ui.Number(grams, 0.1m, 2000, 1);
            var unit=Ui.Combo(["Gram","Adet","Kase","Bardak","Yemek kaşığı"]);var gramsPerUnit=Ui.Number(100,1,2000,1);
            var unitField=Ui.Field("Bir ölçü kaç gram?",gramsPerUnit,160);unitField.IsVisible=false;string previousUnit="Gram";
            var field = (Food: selector, Amount: amount, Unit: unit, GramsPerUnit: gramsPerUnit);
            fields.Add(field);
            var row = Ui.Row(Ui.Field("Yiyecek", selector, 260), Ui.Field("Miktar", amount, 110),Ui.Field("Ölçü",unit,150),unitField);
            row.Children.Add(Ui.Button("Çıkar", () => { fields.Remove(field); rows.Children.Remove(row); ShowTotals(); }));
            selector.SelectionChanged += (_, _) => ShowTotals();
            amount.ValueChanged += (_, _) => ShowTotals();
            gramsPerUnit.ValueChanged+=(_,_)=>ShowTotals();
            unit.SelectionChanged+=(_,_)=>{var total=Ui.Value(amount)*(previousUnit=="Gram"?1:Ui.Value(gramsPerUnit));previousUnit=Ui.Selected(unit);unitField.IsVisible=previousUnit!="Gram";amount.Value=Math.Max(0.1m,Math.Round(total/(previousUnit=="Gram"?1:Math.Max(1,Ui.Value(gramsPerUnit))),1));ShowTotals();};
            rows.Children.Add(row);
            LanguageView.Apply(row);
            ShowTotals();
        }
        foreach (var portion in meal.Portions) AddRow(portion.Food, portion.Grams);
        var panel = Ui.Stack(Ui.Text("Öğünü düzenle", 24, Ui.Ink, true),
            Ui.UserText(date.ToString("dd MMMM dddd") + " • " + AppLanguage.Text(meal.Meal), 16),
            Ui.Field("Öğün adı", title, 500),
            Ui.Text("Yiyecekleri ve gramlarını değiştir. Kalori ve protein toplamı otomatik güncellenir.", 14, Ui.Muted),
            Ui.Text("Adet, kase veya bardak seçersen kullandığın ölçünün gram karşılığını gir. Kap ve yiyecek büyüklüğü değiştiği için tarttığın değeri kullan.",12,Ui.Muted),rows, Ui.Button("Yiyecek ekle", () => { if (choices.Count > 0) AddRow(choices[0], 100); }), totals);
        if (meal.ProteinProduct is {} product)
            panel.Children.Add(Ui.UserText(product.ProductName + " • " + AppLanguage.Text("1 etiket porsiyonu") + " • " + product.ProductCalories + " kcal", 13, Ui.Muted));
        panel.Children.Add(Ui.Text("Kaydetmek, bu öğünü yenmiş olarak işaretlemez. Mevcut beslenme tercihlerin de kaydedilir.", 12, Ui.Muted));
        panel.Children.Add(error);
        var dialog = Ui.Dialog("Öğünü düzenle", panel, 760, 720);
        panel.Children.Add(Ui.Row(Ui.Button("Vazgeç", () => dialog.Close()), Ui.Button("Öğünü kaydet", () =>
        {
            try
            {
                var portions = fields.Select(row => new PersonalMealPortion
                    { Food = (row.Food.SelectedItem as FoodGuideItem)?.Name ?? "", Grams = Grams(row) });
                service.SavePersonalMeal(date, meal.Meal, title.Text ?? "", portions, routine, activity, specialCare);
                saved = true;
                dialog.Close();
            }
            catch (Exception ex) { error.Text = AppLanguage.Text(Ui.ErrorMessage(ex)); }
        }, true)));
        await dialog.ShowDialog(this);
        return saved;
    }
}
