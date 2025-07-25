﻿using System.Collections.Generic;
using UnityEngine.UIElements;
using K2D2.UI;


namespace K2D2.Controller
{
    // a controller that have some sub controler
    public class ComplexController : BaseController
    {
        public List<BaseController> sub_contollers = new List<BaseController>();

        public void setSingleSubController(ComplexController single_sub)
        {
            sub_contollers.Clear();
            if (single_sub != null)
                sub_contollers.Add(single_sub);
        }

        public override void updateUI(VisualElement root_el, FullStatus st)
        {
            // On GUI is used to draw UI in needed, using GUILayout

            foreach (BaseController contoller in sub_contollers)
            {
                contoller.updateUI(root_el, st);
            }
        }

        public override void Update()
        {
            // Update is called each frame

            foreach (BaseController contoller in sub_contollers)
            {
                contoller.Update();
            }
        }

        public override void LateUpdate()
        {
            // Late Update is called just before rendering

            foreach (BaseController contoller in sub_contollers)
            {
                contoller.LateUpdate();
            }
        }

        public override void FixedUpdate()
        {
            // Fixed Update is called on physic update

            foreach (BaseController contoller in sub_contollers)
            {
                contoller.FixedUpdate();
            }
        }
    }
}


