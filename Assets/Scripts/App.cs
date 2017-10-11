using UnityEngine;
using System.Collections;
using Demo.PureMVC.EmployeeAdmin;

public class App : MonoBehaviour {

    public MainWindow window;

    void Start ()
    {
        AppFacade facade = (AppFacade)AppFacade.Instance;
        facade.Startup(window);
    }
}
