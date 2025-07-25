using UnityEngine.UIElements;
using KTools;

namespace K2UI
{
    public class ToggleButton : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<ToggleButton, UxmlTraits> { }

        // Add the two custom UXML attributes.
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_String =
                new() { name = "label", defaultValue = "Toggle Button" };
            UxmlBoolAttributeDescription m_Bool =
                new() { name = "value", defaultValue = false };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var ate = ve as ToggleButton;

                ate.label = m_String.GetValueFromBag(bag, cc);
                ate.Value = m_Bool.GetValueFromBag(bag, cc);
            }
        }

        Label label_el;

        // Must expose your element class to a { get; set; } property that has the same name 
        // as the name you set in your UXML attribute description with the camel case format
        public string _label;
        public string label
        {
            get { return _label; }
            set
            {
                _label = value;
                label_el.text = value;
            }
        }
        bool _value;
        public bool Value
        {
            get { return _value; }
            set
            {
                if (value == _value) return;
                var m_event = ChangeEvent<bool>.GetPooled(_value, value);
                _value = value;
                EnableInClassList(checkedUssClassName, _value);
                m_event.target = this; 
                SendEvent(m_event);
                listeners?.Invoke(value);
            }
        }
        
        public delegate void OnChanged(bool value);

        public event OnChanged listeners;

        public void listen(OnChanged fct)
        {
            listeners += fct;
            fct(Value);
        }

        // In the spirit of the BEM standard, the BigToggleButton has its own block class and two element classes. It also
        // has a class that represents the enabled state of the toggle.
        public static readonly string ussClassName = "toggle-button";
        public static readonly string checkedUssClassName = "checked";


        // This constructor allows users to set the contents of the label.
        public ToggleButton()
        {
            label_el = new Label();
            Add(label_el);

            // Style the control overall.
            AddToClassList(ussClassName);
            this.AddManipulator(new Clickable(evt => {
                ToggleValue();
                }));
        }

        // All three callbacks call this method.
        void ToggleValue()
        {
            Value = !Value;
        }

        // // Because ToggleValue() sets the value property, the BaseField class fires a ChangeEvent. This results in a
        // // call to SetValueWithoutNotify(). This example uses it to style the toggle based on whether it's currently
        // // enabled.
        // public override void SetValueWithoutNotify(bool newValue)
        // {
        //     base.SetValueWithoutNotify(newValue);

        //     //This line of code styles the input element to look enabled or disabled.

        // }


        public void Bind(Setting<bool> setting)
        {
            this.Value = setting.V;
            setting.listeners += v => this.Value = v;
            RegisterCallback<ChangeEvent<bool>>(evt => setting.V = evt.newValue);
        }
        
    }

}