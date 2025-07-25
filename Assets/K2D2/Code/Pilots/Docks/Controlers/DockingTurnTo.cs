using K2D2.KSPService;
using K2D2.Node;
using K2D2.UI;
using KSP.Sim;
using KTools;
using UnityEngine;
using UnityEngine.UIElements;

namespace K2D2.Controller.Docks.Pilots
{
    /// <summary>
    /// rotation used for docking
    /// </summary>
    public class DockingTurnTo : ExecuteController
    {

        KSPVessel current_vessel;

        public float angle;
        public float max_angle;

        public enum Mode
        {
            Off,
            RetroSpeed,
            TargetDock
        }

        public Mode mode = Mode.Off;

        public bool isDockAlign
        {
            get { return mode==Mode.TargetDock; }
        }

        public void StartRetroSpeed(float max_angle = 3)
        {
            mode = Mode.RetroSpeed;
            this.max_angle = max_angle;
            Start();
        }

        public void StartDockAlign(float max_angle = 1)
        {
            mode = Mode.TargetDock;
            this.max_angle = max_angle;
            Start();
        }

        public override void Start()
        {
            current_vessel = K2D2_Plugin.Instance.current_vessel;

            // reset time warp
            TimeWarpTools.SetRateIndex(0, false);
            var autopilot = current_vessel.Autopilot;
            autopilot.Enabled = true;
            autopilot.SetMode(AutopilotMode.StabilityAssist);
        }

        void UpdateRetroSpeed()
        {
            var autopilot = current_vessel.Autopilot;

            // force autopilot
            autopilot.Enabled = true;
            autopilot.SAS.lockedMode = false;

            Vector direction = current_vessel.VesselComponent.TargetVelocity;
            direction.vector = -direction.vector;

            autopilot.SAS.SetTargetOrientation(direction, false);

            finished = false;

            if (!checkRetroSpeed())
                return;

            if (!checkAngularRotation())
                return;

            finished = true;
        }

        Vector wanted_direction;

        void UpdateTargetDock()
        {
            var autopilot = current_vessel.Autopilot;

            // force autopilot
            autopilot.Enabled = true;
            autopilot.SAS.lockedMode = false;


            var target = current_vessel.VesselComponent.TargetObject;
            if (target == null)
            {
                mode = Mode.Off;
            }

            wanted_direction = current_vessel.VesselComponent.TargetObject.transform.up;
            wanted_direction.vector = -wanted_direction.vector;

            autopilot.SAS.SetTargetOrientation(wanted_direction, false);

            finished = false;

            if (!checkTargetDock())
                return;

            if (!checkAngularRotation())
                return;

            finished = true;
        }

        public override void Update()
        {
            switch(mode)
            {
                case Mode.RetroSpeed: UpdateRetroSpeed(); break;
                case Mode.TargetDock: UpdateTargetDock(); break;
            }
        }

        public bool checkRetroSpeed()
        {
            var control_component = current_vessel.VesselComponent.GetControlOwner();
            Vector retro_dir = current_vessel.VesselComponent.TargetVelocity;
            Rotation control_rotation = control_component.transform.Rotation;

            // convert rotation to speed coordinates system
            control_rotation = Rotation.Reframed(control_rotation, retro_dir.coordinateSystem);

            Vector3d forward_direction = (control_rotation.localRotation * Vector3.down).normalized;

            angle = (float)Vector3d.Angle(retro_dir.vector, forward_direction);
            status_line = $"Waiting for good sas direction\nAngle = {angle:n2}°";

            return angle < max_angle;
        }

        public bool checkTargetDock()
        {
            var control_component = current_vessel.VesselComponent.GetControlOwner();

            // Vector retro_dir = current_vessel.VesselComponent.TargetVelocity;
            Rotation control_rotation = control_component.transform.Rotation;

            // convert rotation to speed coordinates system
            control_rotation = Rotation.Reframed(control_rotation, wanted_direction.coordinateSystem);

            Vector3d forward_direction = (control_rotation.localRotation * Vector3.down).normalized;

            angle = (float)Vector3d.Angle(wanted_direction.vector, forward_direction);
            status_line = $"Waiting for good sas direction\nAngle = {angle:n2}°";

            return angle < max_angle;
        }

        public bool checkAngularRotation()
        {
            double max_angular_speed = TurnToSettings.max_angular_speed.V;
            var angular_rotation_pc = current_vessel.GetAngularSpeed().vector;

            status_line = "Waiting for stabilisation";
            if (System.Math.Abs(angular_rotation_pc.x) > max_angular_speed)
                return false;

            if (System.Math.Abs(angular_rotation_pc.y) > max_angular_speed)
                return false;

            if (System.Math.Abs(angular_rotation_pc.z) > max_angular_speed)
                return false;

            return true;
        }

        public override void updateUI(VisualElement root_el, FullStatus st)
        {
            st.Warning("Check Attitude (dockin)");
            st.Console(status_line);

            // UI_Tools.Console($"sas.sas_response v {Tools.print_vector(sas_response)}");

            if (K2D2Settings.debug_mode.V)
            {
                var autopilot = current_vessel.Autopilot;

                // var angulor_vel_coord = VesselInfos.GetAngularSpeed().coordinateSystem;
                var angularVelocity = current_vessel.GetAngularSpeed().vector;

                st.Console($"angle {angle:n2} °");
                st.Console($"angularVelocity {StrTool.Vector3ToString(angularVelocity)}");
                st.Console($"autopilot {autopilot.AutopilotMode}");
            }
        }
    }
}
