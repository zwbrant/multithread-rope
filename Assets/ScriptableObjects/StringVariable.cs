// ----------------------------------------------------------------------------
// Unite 2017 - Game Architecture with Scriptable Objects
// 
// Author: Ryan Hipple
// Date:   10/04/17
// ----------------------------------------------------------------------------

using UnityEngine;

namespace RoboRyanTron.Unite2017.Variables
{
    [CreateAssetMenu(fileName = "StringVariable", menuName = "ScriptObjs/String", order = 1)]
    public class StringVariable : PrintableScriptObj
    {
        [SerializeField]
        private string value = "";

        public string Value
        {
            get { return value; }
            set { this.value = value; }
        }

        public override string ToString()
        {
            return Value;
        }
    }
}