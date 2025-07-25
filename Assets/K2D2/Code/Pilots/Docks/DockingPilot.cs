using K2D2.KSPService;
using KSP.Sim.impl;

using K2D2.Controller.Docks;
using static K2D2.Controller.Docks.DockTools;
using K2D2.Controller.Docks.Pilots;
using ReduxLib.Logging;

namespace K2D2.Controller
{
    public class DockingPilot : SingleExecuteController
    {
        public static DockingPilot Instance { get; set; }
        public ILogger logger = ReduxLib.ReduxLib.GetLogger("K2D2.DockingTool");

        public KSPVessel current_vessel;
        public NamedComponent control_component = null;
        public VesselComponent target_vessel;
        public NamedComponent target_part;

        // public int target_dock_num = -1;
        public SimulationObjectModel last_target;


        public enum PilotMode
        {
            Off,
            MainThrustKillSpeed,
            RCSFinalApproach,
        }

        PilotMode _mode = PilotMode.Off;
        public PilotMode Mode
        {
            get
            {
                return _mode;
            }
            set
            {
                if (_mode == value) return;
                switch(value)
                {
                    case PilotMode.MainThrustKillSpeed:
                        if (target_vessel == null) return;
                        setController(kill_speed_pilot);
                        kill_speed_pilot.Start();
                        isRunning = true;
                        break;
                    case PilotMode.RCSFinalApproach:
                        if (target_vessel == null) return;

                        setController(final_approach_pilot);
                        final_approach_pilot.StartPilot(target_part, control_component);
                        isRunning = true;
                        break;
                    case PilotMode.Off:
                        setController(null);
                        isRunning = false;
                        break;
                }

                _mode = value;
            }
        }

        public MainThrustKillSpeed kill_speed_pilot = null;
        public FinalApproach final_approach_pilot = null;

        public override bool isRunning
        {
            get { return base.isRunning; }
            set
            {
                if (value == base.isRunning)  return;
                // send call backs
                base.isRunning = value; 

                if (!value)
                {
                    current_vessel = K2D2_Plugin.Instance.current_vessel;
                    // stop
                    if (current_vessel != null)
                        current_vessel.SetThrottle(0);

                    Mode = PilotMode.Off;
                } 
            }
        }

        public override void onReset()
        {
            isRunning = false;
        }

        public DockingPilot()
        {
            Instance = this;
            debug_mode_only = false;
            K2D2PilotsMgr.Instance.RegisterPilot("Dock", this);

            current_vessel = K2D2_Plugin.Instance.current_vessel;

            _page = new DockingUI(this);

            kill_speed_pilot = new MainThrustKillSpeed(turnTo);
            final_approach_pilot = new FinalApproach(this, turnTo);
        }

        public DockingTurnTo turnTo = new DockingTurnTo();

        // public void listDocks()
        // {
        //     docks = FindParts(target_vessel, false, true);
        // }

        public override void Update()
        {
            if (!isRunning && ! page.isVisible)
                return;

            turnTo.Update();

            // logger.LogInfo($"target is {current_vessel.VesselComponent.TargetObject}");
            if (current_vessel.VesselComponent == null)
            {
                control_component = null;
                return;
            }

            if (sub_controler != null)
            {
                sub_controler.Update();
                if (sub_controler.finished)
                {
                    isRunning = false;
                }
            }

            control_component = new NamedComponent(current_vessel.VesselComponent.GetControlOwner());

            // update the dock when target change
            if (last_target != current_vessel.VesselComponent.TargetObject)
            {
                // logger.LogInfo($"changed target is {current_vessel.VesselComponent.TargetObject}");

                last_target = current_vessel.VesselComponent.TargetObject;
                target_part = null;
                // target_dock_num = -1;

                if (last_target == null)
                {
                    target_vessel = null;
                
                    //docks.Clear();
                }
                else if (last_target.IsCelestialBody)
                {
                    //docks.Clear();
                }
                else if (last_target.IsVessel)
                {
                    // logger.LogInfo(last_target);
                    target_vessel = last_target.Vessel;
                    target_part = new NamedComponent(target_vessel.GetControlOwner());
                }
                else if (last_target.IsPart)
                {
                    // dock selected
                    target_part = new NamedComponent(last_target.Part);
                    // if (docks.Count == 0 && target_vessel != null)
                    //     listDocks();

                    // target_dock_num = docks.IndexOf(last_target.Part) + 1;

                    PartOwnerComponent owner = target_part.component.PartOwner;
                    if (owner.SimulationObject.IsVessel)
                    {
                        target_vessel = owner.SimulationObject.Vessel;
                    }
                }
                else
                {
                    target_vessel = null;
                }
            }
        }

        // public void drawShapes()
        // {
        //     if (dock_ui.drawShapes(shapes_drawer))
        //         return;

        //     var vessel = current_vessel.VesselComponent;
        //     if (settings.show_gizmos)
        //     {
        //         if (target_part != null)
        //         {
        //             // draw target
        //             shapes_drawer.DrawComponent(target_part, vessel, settings.target_color, true, true);

        //             // draw target speed
        //             shapes_drawer.DrawSpeed(control_component, vessel, vessel.TargetVelocity, Color.red);
        //         }

        //         // draw control
        //         shapes_drawer.DrawComponent(control_component, vessel, settings.vessel_color, true, false);
        //         shapes_drawer.drawAxis(control_component, vessel);
        //     }

        //     if (Mode == PilotMode.RCSFinalApproach)
        //     {
        //         final_approach_pilot.drawShapes(shapes_drawer);
        //     }
        // }
    }
}