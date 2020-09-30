using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TeamBlack.MoonShot.UI
{
    
    public class EscMenu : MonoBehaviour
    {
        private ShowHide _sh;

        private bool __active = true;

        private bool _active
        {
            set
            { 
                var y = __active ? -10000 : 10000;
                if (value != __active) transform.position += new Vector3(0, y,0);
                __active = value;
            }
            get { return __active; }
        }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Q))
            {
                ToggleMenu();
            }
        }

        void Awake()
        {
            _sh = GetComponent<ShowHide>();
            _active = false;
        }

        public void ToggleMenu()
        {
            _active = !_active;
            //            _sh?.ToggleMenu();
        }

        public void GotoMenu()
        {
            SceneManager.LoadScene(0);
        }

        public void ExitApplication()
        {
            Application.Quit();
        }
    }
}