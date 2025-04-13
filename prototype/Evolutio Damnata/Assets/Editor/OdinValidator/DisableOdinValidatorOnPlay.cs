using UnityEngine;
using UnityEditor;
using System.Reflection;

[InitializeOnLoad]
public class DisableOdinValidatorOnPlay
{
    static DisableOdinValidatorOnPlay()
    {
        // Try to find Odin Validator assembly
        Assembly odinValidatorAssembly = null;
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.GetName().Name == "Sirenix.OdinValidator.Editor")
            {
                odinValidatorAssembly = assembly;
                break;
            }
        }

        if (odinValidatorAssembly != null)
        {
            // Get the AutomationConfig type
            var automationConfigType = odinValidatorAssembly.GetType("Sirenix.OdinValidator.Editor.AutomationConfig");
            if (automationConfigType != null)
            {
                // Get the instance property
                var instanceProperty = automationConfigType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                if (instanceProperty != null)
                {
                    var instance = instanceProperty.GetValue(null);
                    if (instance != null)
                    {
                        // Get the ValidateOnPlay property
                        var validateOnPlayProperty = automationConfigType.GetProperty("ValidateOnPlay");
                        if (validateOnPlayProperty != null)
                        {
                            // Set ValidateOnPlay to false
                            validateOnPlayProperty.SetValue(instance, false);
                            Debug.Log("Odin Validator - 'Validate On Play' has been disabled programmatically");
                        }
                    }
                }
            }
        }
    }
} 