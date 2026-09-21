using CardGame.DeckManagement;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace CardGame.CardEditor.Components.Pages;

public partial class Home : ComponentBase
{
    private const string CatalogPath = "../CardGame.Web/wwwroot/data/cards.json";
    private const string ImagesDirectory = "../CardGame.Web/wwwroot/images";

    private const string NameColumn = "Name";
    private const string CostColumn = "Cost";
    private const string ChargeColumn = "Charge";
    private const string AttackColumn = "Attack";
    private const string DefenseColumn = "Defense";

    private List<CardTemplate> _templates = [];
    private CardFormModel _form = new();
    private IBrowserFile? _selectedImage;
    private bool _isFormVisible;
    private int? _editingIndex;
    private string? _errorMessage;
    private string? _sortColumn;
    private bool _sortAscending = true;

    protected override void OnInitialized()
    {
        try
        {
            _templates = CardCatalog.Load(CatalogPath);
        }
        catch (FileNotFoundException)
        {
            _templates = [];
        }
    }

    private void StartAdd()
    {
        _isFormVisible = true;
        _editingIndex = null;
        _form = new CardFormModel();
        _selectedImage = null;
    }

    private void StartEdit(int index)
    {
        var template = _templates[index];
        _isFormVisible = true;
        _editingIndex = index;
        _form = new CardFormModel
        {
            Name = template.Name,
            Cost = template.Cost,
            Charge = template.Charge,
            Attack = template.Attack,
            Defense = template.Defense,
            ImagePath = template.ImagePath,
        };
        _selectedImage = null;
    }

    private void CancelEdit()
    {
        _isFormVisible = false;
        _editingIndex = null;
    }

    private void OnImageSelected(InputFileChangeEventArgs e)
    {
        _selectedImage = e.File;
    }

    private async Task Save()
    {
        _errorMessage = null;

        try
        {
            var imagePath = _form.ImagePath;

            if (_selectedImage != null)
            {
                Directory.CreateDirectory(ImagesDirectory);
                var destinationPath = Path.Combine(ImagesDirectory, _selectedImage.Name);

                await using var destinationStream = File.Create(destinationPath);
                await using var sourceStream = _selectedImage.OpenReadStream();
                await sourceStream.CopyToAsync(destinationStream);

                imagePath = $"images/{_selectedImage.Name}";
            }

            var template = new CardTemplate(
                _form.Name,
                _form.Cost,
                _form.Charge,
                _form.Attack,
                _form.Defense,
                imagePath
            );

            if (_editingIndex is int index)
            {
                _templates[index] = template;
            }
            else
            {
                _templates.Add(template);
            }

            CardCatalog.Save(CatalogPath, _templates);
            CancelEdit();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Couldn't save: {ex.Message}";
        }
    }

    private void Delete(int index)
    {
        _templates.RemoveAt(index);
        CardCatalog.Save(CatalogPath, _templates);
    }

    private string SortIndicator(string column)
    {
        if (_sortColumn != column)
        {
            return "";
        }

        return _sortAscending ? " ▲" : " ▼";
    }

    private void SortBy(string column)
    {
        _sortAscending = _sortColumn == column ? !_sortAscending : true;
        _sortColumn = column;

        Func<CardTemplate, IComparable> keySelector = column switch
        {
            NameColumn => t => t.Name,
            CostColumn => t => t.Cost,
            ChargeColumn => t => t.Charge,
            AttackColumn => t => t.Attack,
            DefenseColumn => t => t.Defense,
            _ => t => t.Name,
        };

        _templates = _sortAscending
            ? _templates.OrderBy(keySelector).ToList()
            : _templates.OrderByDescending(keySelector).ToList();
    }

    private sealed class CardFormModel
    {
        public string Name { get; set; } = "";
        public int Cost { get; set; }
        public int Charge { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public string? ImagePath { get; set; }
    }
}
