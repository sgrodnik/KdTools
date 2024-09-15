using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using KdTools.BASE;

namespace KdTools.Template;

internal class ViewModel : BASE.ViewModel
{
    private readonly ExternalCommandData _commandData;
    private View _view;
    private readonly Model _model;

    private string _title = new Command().Title;
    public string Title
    {
        get => _title;
        set => Set(ref _title, value);
    }

    private static bool _boolFlagOne = true;
    public bool BoolFlagOne
    {
        get
        {
            Model.BoolFlagOne = _boolFlagOne;
            return _boolFlagOne;
        }
        set
        {
            if (IsStrangeImpact()) return;
            Set(ref _boolFlagOne, value);
            Model.BoolFlagOne = value;
        }
    }

    // It's a dirty fix of a bug.
    // To reproduce the bug switch this check off and close the plugin window with ordinary Close Button (the right
    // upper cross icon) and run it again. Radiobuttons is unchecked. Something magical switches them off.
    // Bun when you close the window with the Run button, nothing strange happens.
    private bool IsStrangeImpact()
    {
        return _view?.IsVisible != true;
    }

    private static bool _boolFlagTwo;
    public bool BoolFlagTwo
    {
        get => _boolFlagTwo;
        set
        {
            if (IsStrangeImpact()) return;
            Set(ref _boolFlagTwo, value);
        }
    }

    private static bool _boolFlagThree;
    public bool BoolFlagThree
    {
        get => _boolFlagThree;
        set {
            if (IsStrangeImpact()) return;
            Set(ref _boolFlagThree, value);
        }
    }

    private static bool _isCeilingSelected = true;
    public bool IsCeilingSelected
    {
        get
        {
            Model.IsCeilingSelected = _isCeilingSelected;
            return _isCeilingSelected;
        }
        set
        {
            Set(ref _isCeilingSelected, value);
            Model.IsCeilingSelected = value;
        }
    }

    private List<CeilingType> _ceilingTypes;
    public List<CeilingType> CeilingTypes
    {
        get => _ceilingTypes;
        set => Set(ref _ceilingTypes, value);
    }

    private ElementId _selectedCeilingTypeId;
    public ElementId SelectedCeilingTypeId
    {
        get => _selectedCeilingTypeId;
        set {
            Set(ref _selectedCeilingTypeId, value);
            _model.CeilingTypeId = value;
        }
    }

    private static string _someStrVar = "Default";
    public string SomeStrVar
    {
        get
        {
            Model.SomeStrVar = _someStrVar;
            return _someStrVar;
        }
        set
        {
            Set(ref _someStrVar, value);
            Model.SomeStrVar = value;
        }
    }

    public int AnIntegerCount
    {
        get => _model.AnIntegerCount;
        set { }
    }

    public View View
    {
        get
        {
            if (_view is not null) return _view;
            _view = new View {DataContext = this};
            new WindowInteropHelper(_view).Owner = _commandData.Application.MainWindowHandle;
            return _view;
        }
        set => Set(ref _view, value);
    }

    public ICommand ButtonRun { get; }
    public ICommand ButtonCancel { get; }
    
    public ViewModel() { }

    public ViewModel(ExternalCommandData commandData)
    {
        _commandData = commandData;
        _model = new Model(_commandData);
        
        ButtonRun = new LambdaCommand(p =>
        {
            View.Close();
            _model.DoJob();
        }, _ => true);
        ButtonCancel = new LambdaCommand(p => View.Close(), _ => true);
    }

    public ImageSource Icon { get; } = Utils.GetImageSource(new Command().Logo, 16);
}
