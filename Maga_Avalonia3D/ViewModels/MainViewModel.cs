using Avalonia;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using ModelStub;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Input;


namespace Maga_Avalonia3D.ViewModels;

public class MainViewModel : ViewModelBase, INotifyPropertyChanged
{
    private int _pointNum = 500;
    public ICommand GaussianCommand { get; }
    public ICommand SaddleCommand { get; }
    public ICommand HyperboloidCommand { get; }
    public MainViewModel()
    {
        GaussianCommand = new RelayCommand(() => { Points = Surfaces.Gaussian(_pointNum); });
        SaddleCommand = new RelayCommand(() => { Points = Surfaces.Saddle(_pointNum); });
        HyperboloidCommand = new RelayCommand(() => { Points = Surfaces.Ripple(_pointNum); });

        Points = Surfaces.Gaussian(_pointNum);
    }

    private List<Vector3> _points = new List<Vector3>();
    public List<Vector3> Points
    {
        get => _points;
        set { _points = value; OnPropertyChanged(); }
    }

    private float _yaw = 2.4f;
    public float Yaw
    {
        get => _yaw;
        set { _yaw = value; OnPropertyChanged(); }
    }

    private float _pitch = 0.1f;
    public float Pitch
    {
        get => _pitch;
        set { _pitch = value; OnPropertyChanged(); }
    }

    private float _distance = 1.1f;
    public float Distance
    {
        get => _distance;
        set { _distance = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
