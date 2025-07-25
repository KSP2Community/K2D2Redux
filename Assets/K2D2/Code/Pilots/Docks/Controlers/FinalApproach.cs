
using K2D2.KSPService;
using K2D2.UI;
using K2UI;
using KTools;
using UnityEngine;
using UnityEngine.UIElements;
using static K2D2.Controller.Docks.DockTools;

namespace K2D2.Controller.Docks.Pilots
{
    /// <summary>
    /// rotation used for docking
    /// </summary>
    public class FinalApproach : ExecuteController
    {
        public FinalApproach(DockingPilot pilot, DockingTurnTo turnTo)
        {
            this.pilot = pilot;
            this.turnTo = turnTo;
        }

        KSPVessel current_vessel;
        private DockingPilot pilot;
        DockingTurnTo turnTo = null;

        public NamedComponent control = null;

        public NamedComponent target_part;

        public void StartPilot(NamedComponent target, NamedComponent control)
        {
            base.Start();
            this.target_part = target;
            this.control = control;
            current_vessel = K2D2_Plugin.Instance.current_vessel;
            turnTo.StartDockAlign();

            sub_mode.V = SubMode.Manual;

            finished = false;
        }

        public enum SubMode
        {
            Manual,
            Auto,
        }


        public EnumSetting<SubMode> sub_mode = new("dock.final_mode", SubMode.Manual);
        public ClampSetting<float> rcs_power = new ClampSetting<float>("dock.rc_kill_power", 1, 0 , 10);

        ClampSetting<float> forward_speed = new("", 0, -10, 10);

        public Setting<bool> Kill_X_Speed = new Setting<bool>("", false);
        public Setting<bool> Kill_Y_Speed = new Setting<bool>("", false);
        public Setting<bool> Kill_Z_Speed = new Setting<bool>("", false);

        ClampSetting<float> center_power = new("", 1, 0, 5);

        public Vector3 local_speed = new Vector3();
    
        public Vector3 vessel_to_target = new Vector3();

        public override void Update()
        {
            finished = false;

            current_vessel.SetThrottle(0);

            // update target position and speed
            UpdatePosition();

            switch (sub_mode.V)
            {
                case SubMode.Manual:
                    RCKillSpeed();
                    break;
                case SubMode.Auto:
                    AutoMode();
                    break;
            }
        }

        void RCKillSpeed()
        {
            if (Kill_X_Speed.V && current_vessel.X == 0)
                current_vessel.X = -local_speed.x * rcs_power.V;

            if (Kill_Y_Speed.V && current_vessel.Y == 0)
                current_vessel.Y = -local_speed.y * rcs_power.V;

            if (Kill_Z_Speed.V && current_vessel.Z == 0)
                current_vessel.Z = -local_speed.z * rcs_power.V;
        }

        void RCKillSpeed_UI(FullStatus st)
        {
            st.Console("<b>Kill Speed using RCS</b>");

            if (K2D2Settings.debug_mode.V)
            {
                st.Console($"Horizontal {local_speed.x:n2} " + (Kill_X_Speed.V ? "[X]":"[ ]"));
                st.Console($"Vertical {local_speed.x:n2} " + (Kill_Z_Speed.V ? "[X]":"[ ]"));
                st.Console($"Depth {local_speed.x:n2} " + (Kill_Y_Speed.V ? "[X]":"[ ]"));
            } 
        }

        Setting<bool> auto_forward = new("", false);

        Setting<bool> center_axis = new("", false);


        float forward_wanted_speed;

        float right_wanted_speed;
        float up_wanted_speed;

        void AutoMode()
        {
            float max_speed = 1 + vessel_to_target.magnitude / 10;

            if (auto_forward.V)
            {
                forward_wanted_speed = Mathf.Sign(vessel_to_target.y) * Mathf.Sqrt(Mathf.Abs(center_power.V / 20 * vessel_to_target.y));
                forward_wanted_speed += 0.05f; // final touch speed !!
                forward_wanted_speed = Mathf.Clamp(forward_wanted_speed, -max_speed, max_speed);
                current_vessel.Y = (forward_wanted_speed - local_speed.y) * rcs_power.V;
            }
            else
            {
                forward_wanted_speed = forward_speed.V;
                current_vessel.Y = (forward_speed.V - local_speed.y) * rcs_power.V;
            }

            if (center_axis.V)
            {
                right_wanted_speed = Mathf.Sign(vessel_to_target.x) * Mathf.Sqrt(Mathf.Abs(center_power.V / 10 * vessel_to_target.x));
                right_wanted_speed = Mathf.Clamp(right_wanted_speed, -max_speed, max_speed);
                current_vessel.X = (right_wanted_speed - local_speed.x) * rcs_power.V;

                up_wanted_speed = Mathf.Sign(vessel_to_target.z) * Mathf.Sqrt(Mathf.Abs(center_power.V / 10 * vessel_to_target.z));
                up_wanted_speed = Mathf.Clamp(up_wanted_speed, -max_speed, max_speed);
                current_vessel.Z = (up_wanted_speed - local_speed.z) * rcs_power.V;
            }
        }

        void AutoMode_UI(FullStatus st)
        {  
            st.Console("<b>Semi-Auto Dock pilot</b>");

            if (K2D2Settings.debug_mode.V)
            {
                if (center_axis.V)
                {
                    st.Console($"<b>Horizontal</b>- ctr:{right_wanted_speed:n2} ms ~ cur:{local_speed.x:n2} ms");
                    st.Console($"<b>Vertical</b>- ctr:{up_wanted_speed:n2} ms ~ cur:{local_speed.z:n2} ms");
                }
                st.Console($"<b>Forward</b> - ctr:{forward_wanted_speed:n2} ms ~ cur:{local_speed.y:n2} ms");
            }
     
        }

        void UpdatePosition()
        {
            if (control == null)
                return;

            if (target_part != null)
            {
                var vessel = current_vessel.VesselComponent;
                if (vessel == null)
                {
                    return;
                }

                // diff_Position = Position.Delta(target_part.CenterOfMass, control_component.CenterOfMass);
                var curent_vessel_frame = control.component.transform.coordinateSystem;

                var vessel_to_control = Matrix4x4D.TRS(
                    curent_vessel_frame.ToLocalPosition(control.component.transform.Position),
                    curent_vessel_frame.ToLocalRotation(control.component.transform.Rotation)).GetInverse();

                vessel_to_target = vessel_to_control.TransformPoint(curent_vessel_frame.ToLocalPosition(target_part.component.CenterOfMass));

                local_speed = vessel_to_control.TransformVector(curent_vessel_frame.ToLocalVector(vessel.TargetVelocity));

                if (last_TargetSpeed != Vector3.zero)
                {
                    var delta = local_speed - last_TargetSpeed;
                    current_acc = delta / Time.deltaTime;
                }

                last_TargetSpeed = local_speed;
            }
        }

        Vector3 rcsThrust = Vector3.one;
        Vector3 last_TargetSpeed;
        Vector3 current_acc;

        // void computeRCSThrust()
        // {
        //     // Why do X Y Z does not fit the speed direction ? rotation of 90° X ...
        //     if (current_vessel.X != 0)
        //     {
        //         rcsThrust.x = Mathf.Lerp(rcsThrust.x, current_acc.x / current_vessel.X, Time.deltaTime * 1);
        //     }
        //     if (current_vessel.Y != 0)
        //     {
        //         rcsThrust.y = Mathf.Lerp(rcsThrust.y, current_acc.y / current_vessel.Y, Time.deltaTime * 1);
        //     }
        //     if (current_vessel.Z != 0)
        //     {
        //         rcsThrust.z = Mathf.Lerp(rcsThrust.z, current_acc.z / current_vessel.Z, Time.deltaTime * 1);
        //     }
        // }

        VisualElement final_approach_group;
        InlineEnum final_mode;

        Group manual_group;
        Group auto_group;

        void UpdateKillAll()
        {
            bool all = Kill_X_Speed.V && Kill_Y_Speed.V && Kill_Z_Speed.V;
            kill_all_speed.Value = all;
        }

        ToggleButton kill_all_speed;
        K2Slider center_power_el, forward_speed_el;
        private VisualElement reset_fspeed;

        public void Hide()
        {
            final_approach_group.Show(false);
        }

        public void onInitUI(VisualElement panel, VisualElement settings_page)
        {
            final_approach_group = panel.Q<VisualElement>("final_approach_group");
            final_approach_group.Show(false);

            final_mode = final_approach_group.Q<InlineEnum>("final_mode");
            final_mode.Bind(sub_mode);

            manual_group = final_approach_group.Q<Group>("manual_group");
            auto_group = final_approach_group.Q<Group>("auto_group");

            // reset forward speed when going to Auto
            sub_mode.listen((v) =>
            {
                if (v == SubMode.Auto)
                {
                    manual_group.Show(false);
                    auto_group.Show(true);
                    forward_speed.V = 0;
                }
                else
                {
                    manual_group.Show(true);
                    auto_group.Show(false);
                    Kill_X_Speed.V = Kill_Y_Speed.V = Kill_Z_Speed.V = false;
                }
            });

            manual_group.Q<ToggleButton>("horizontal_bt").Bind(Kill_X_Speed);
            manual_group.Q<ToggleButton>("vertical_bt").Bind(Kill_Z_Speed);
            manual_group.Q<ToggleButton>("depth_bt").Bind(Kill_Y_Speed);

            kill_all_speed = manual_group.Q<ToggleButton>("all_bt");
            kill_all_speed.listenClick(() =>
                {
                    Kill_X_Speed.V = Kill_Y_Speed.V = Kill_Z_Speed.V = kill_all_speed.Value;
                }
            );

            Kill_X_Speed.listen((v) => UpdateKillAll());
            Kill_Y_Speed.listen((v) => UpdateKillAll());
            Kill_Z_Speed.listen((v) => UpdateKillAll());

            auto_group.Q<K2Toggle>("center_axis").Bind(center_axis);
            auto_group.Q<K2Toggle>("auto_forward").Bind(auto_forward);
            center_power_el = auto_group.Q<K2Slider>("center_power").Bind(center_power);
            forward_speed_el = auto_group.Q<K2Slider>("forward_speed").Bind(forward_speed);
            reset_fspeed = auto_group.Q<Button>("reset_fspeed").listenClick(() => forward_speed.V = 0);

            settings_page.Q<K2Slider>("rcs_power").Bind(rcs_power);


        }

        public override void updateUI(VisualElement root_el, FullStatus st)
        {
            final_approach_group.Show(true);
            st.Warning("Final Approach");

            center_power_el.Show(center_axis.V || auto_forward.V);
            forward_speed_el.Show(!auto_forward.V);
            reset_fspeed.Show(!auto_forward.V);

            switch (sub_mode.V)
            {
                case SubMode.Manual:
                    RCKillSpeed_UI(st);
                    break;
                case SubMode.Auto:
                    AutoMode_UI(st);
                    break;
            }

            if (K2D2Settings.debug_mode.V)
            {
                st.Console($"speed {StrTool.Vector3ToString(local_speed)} ");
                // UI_Tools.Console($"Rcs Thrust {StrTool.Vector3ToString(rcsThrust)} ");
                st.Console($"Control {current_vessel.X:n2} {current_vessel.Y:n2} {current_vessel.Z:n2}");
                st.Console($"<b>Pos {StrTool.Vector3ToString(vessel_to_target)} </b>");
            }
        }

        // public void drawShapes(DockShape shape_drawer)
        // {
        //     drawTargetPosLines(shape_drawer);
        // }

        // public void drawTargetPosLines(DockShape shape_drawer)
        // {
        //     // draw directions of position in the local control frame
        //     if (control_component == null)
        //         return;

        //     //L.Log("Final Approach Draw Shapes");

        //     if (settings.show_gizmos)
        //     {
        //         Position center = control_component.CenterOfMass;
        //         Position start = center + control_component.transform.up * settings.pos_grid;

        //         Vector3 pos = vessel_to_target;

        //         // start with X
        //         Position XPos = start + control_component.transform.right * pos.x;

        //         // Z is the Y command
        //         Position Center_Plane = XPos + control_component.transform.forward * pos.z;

        //         // y is the z command
        //         Position ZPos = Center_Plane + control_component.transform.up * pos.y;


        //         float len_lines = 100;

        //         // vertical lines
        //         shape_drawer.Drawline(Center_Plane ,
        //                             Center_Plane + control_component.transform.forward*len_lines,
        //                             current_vessel.VesselComponent, Color.green);

        //         shape_drawer.Drawline(Center_Plane ,
        //                             Center_Plane + control_component.transform.back*len_lines,
        //                             current_vessel.VesselComponent, Color.green);

        //         // horizontal line
        //         shape_drawer.Drawline(Center_Plane,
        //                             Center_Plane + control_component.transform.left*len_lines,
        //                             current_vessel.VesselComponent, Color.blue);

        //         shape_drawer.Drawline(Center_Plane,
        //                             Center_Plane + control_component.transform.right*len_lines,
        //                             current_vessel.VesselComponent, Color.blue);
        //         // depth line
        //         shape_drawer.Drawline(Center_Plane, ZPos, current_vessel.VesselComponent, Color.yellow);
        //     }
        // }
    }
}
