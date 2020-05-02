// ----------------------------------------------------------------------------
// Unite 2017 - Game Architecture with Scriptable Objects
// 
// Author: Ryan Hipple
// Date:   10/04/17
// ----------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;

namespace RoboRyanTron.Unite2017.Variables
{
    public class TextReplacer : MonoBehaviour
    {
        public Text Text;
        public InputField InputField;

        [SerializeField]
        public PrintableScriptObj Variable;

        public bool AlwaysUpdate;
        
        private void OnEnable()
        {

            Text.text = Variable.ToString();
            if (InputField != null)
                InputField.text = Variable.ToString();
        }

        private void Update()
        {
            if (AlwaysUpdate)
            {
                Text.text = Variable.ToString();
                if (InputField != null)
                    InputField.text = Variable.ToString();
            }
        }
    }
}