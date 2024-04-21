using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Linq;
using VTOLAPICommons;
using VTOLAPICommons.WinTrust;

namespace IMLLoader
{
    public class VTPatcher
    {

        private string gamePath;
        private bool disableVr;
        private bool addGetterSetters;

        public VTPatcher(string path, bool disableVr, bool addGetterSetters)
        {
            this.gamePath = path;
            this.disableVr = disableVr;
            this.addGetterSetters = addGetterSetters;
        }

        public void Start()
        {
            if (!IsValidGame())
            {
                Logger.Log("Please use the steam version of VTOL to play with mods");
                UnityEngine.Application.Quit();
                return;
            }

            Logger.Log($"Beginning DLL patch. Reading from {gamePath}");
            PatchVTOL();
            Logger.Log("DLL Patch finished!");
        }

        private bool IsValidGame()
        {
            string steamApiPath = gamePath + "/VTOLVR_Data/Plugins/steam_api64.dll";
            if (File.Exists(steamApiPath)) return IsTrusted(steamApiPath);

            steamApiPath = Path.Combine(gamePath + "/VTOLVR_Data/Plugins/x86_64/steam_api64.dll");
            if (File.Exists(steamApiPath)) return IsTrusted(steamApiPath);

            return true;
        }


        private void PatchVTOL()
        {
            var dllPath = gamePath + "/VTOLVR_Data/Managed/Assembly-CSharp.dll";
            ModuleDefinition module = ModuleDefinition.ReadModule(dllPath, new ReaderParameters()
            {
                ReadWrite = false,
                InMemory = true,
                ReadingMode = ReadingMode.Immediate,
                AssemblyResolver = new CustomResolver(gamePath + "/VTOLVR_Data/Managed")
            });

            foreach (var type in module.Types)
            {
                VirtualizeType(type, ref module);
            }

            module.Write("./VTOLVR_Data/Managed/Assembly-CSharp.dll");
            module.Dispose();
        }

        private void VirtualizeType(TypeDefinition type, ref ModuleDefinition module)
        {
            if (type.IsSealed) type.IsSealed = false;

            if (type.IsNestedPrivate)
            {
                type.IsNestedPrivate = false;
                type.IsNestedPublic = true;
            }

            if (type.IsInterface || type.IsAbstract) return;

            foreach (var nestedType in type.NestedTypes)
            {
                VirtualizeType(nestedType, ref module);
            }

            foreach (MethodDefinition method in type.Methods)
            {
                if (method.IsManaged
                    && method.IsIL
                    && !method.IsStatic
                    && (!method.IsVirtual || method.IsFinal)
                    && !method.IsAbstract
                    && !method.IsAddOn
                    && !method.IsConstructor
                    && !method.IsSpecialName
                    && !method.IsGenericInstance
                    && !method.HasOverrides)
                {
                    method.IsVirtual = true;
                    method.IsFinal = false;
                    method.IsPublic = true;
                    method.IsPrivate = false;
                    method.IsNewSlot = true;
                    method.IsHideBySig = true;
                }

                if (type.Name.Equals(nameof(GameVersion)) && method.Name.Equals(nameof(GameVersion.ConstructFromValue)))
                {
                    Logger.Log($"Patching GameVersion.ConstructFromValue");
                    PatchGameVersion(method);
                }

                if (type.Name.Equals(nameof(XRLoaderSelector)) && method.Name.Equals("LoadXR") && disableVr)
                {
                    Logger.Log($"Patching XRLoaderSelector.LoadXR");
                    PatchLoadXR(method);
                }

                if (type.Name.Equals(nameof(SplashSceneController)) && method.Name.Equals("Start"))
                {
                    Logger.Log($"Patching SplashSceneController.Start to disable the old mod loader");
                    RevertModloaderPatch(method);
                }
            }

            foreach (var field in type.Fields)
            {
                if (field.IsPrivate)
                {
                    field.IsFamily = true;
                    // field.IsPrivate = false;
                    // field.IsPublic = true;

                    if (addGetterSetters)
                    {
                        // Setup setter
                        var md = new MethodDefinition("Get_" + field.Name, MethodAttributes.Public | MethodAttributes.HideBySig, field.FieldType);
                        md.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                        md.Body.Instructions.Add(Instruction.Create(OpCodes.Ldfld, field));
                        md.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                        type.Methods.Add(md);

                        // Setup getter
                        md = new MethodDefinition("Set_" + field.Name, MethodAttributes.Public | MethodAttributes.HideBySig, module.TypeSystem.Void);
                        md.Parameters.Add(new ParameterDefinition(field.FieldType));
                        md.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                        md.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
                        md.Body.Instructions.Add(Instruction.Create(OpCodes.Stfld, field));
                        md.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                        type.Methods.Add(md);
                    }
                }

            }

            // What is this commented code? Can it be removed? - Ierdna
/*            if (addGetterSetters)
            {
                foreach (var evt in type.Events)
                {
                    // if (evt.Name != "OnDamageLevel") continue;
                    Logger.Log($"Patching event {evt.Name}");
                    Logger.Log($"Event type: {evt.EventType.Name}");
                    var md = new MethodDefinition("Exec_" + evt.Name, MethodAttributes.Public | MethodAttributes.HideBySig, module.TypeSystem.Void);
                    var invokeMethod = evt.EventType.Resolve().Methods.Where(m =>
                    {
                        Logger.Log($"Event method {m.Name}");
                        return m.Name == "Invoke";
                    }).First();

                    // Add params based off event type generics
                    foreach (var eventParameter in invokeMethod.Parameters)
                    {
                        Logger.Log($"Event type generic param: {eventParameter.Name}");
                        md.Parameters.Add(new ParameterDefinition(eventParameter.ParameterType));
                    }

                    FieldDefinition fd = null;
                    foreach (var field in type.Fields)
                    {
                        if (field.Name == evt.Name) fd = field;
                    }

                    if (fd == null)
                    {
                        Logger.Log($"Unable to find field for event {evt.Name}");
                        return;
                    }


                    // Load event onto stack
                    Logger.Log($"Adding load onto stack call");
                    md.Body.Instructions.Add(Instruction.Create(OpCodes.Ldsfld, fd));
                    // Load arguments
                    for (int i = 0; i < evt.EventType.GenericParameters.Count; i++)
                    {
                        md.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg, i + 1));
                    }

                    // Call invoke
                    Logger.Log($"Adding call to invoke");
                    // var method = evt.EventType.
                    var importedInvokeMethod = type.Module.ImportReference(invokeMethod);
                    Logger.Log($"Imported invoke method: {importedInvokeMethod}");
                    md.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, importedInvokeMethod));

                    md.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

                    // Logger.Log($"Adding import to {invokeMethod.FullName}, via {invokeMethod.DeclaringType}");
                    // type.Module.ImportReference(typeof(System.Action<int>.Invoke));
                    type.Methods.Add(md);
                }
            }*/
        }

        private void PatchLoadXR(MethodDefinition method)
        {
            var ilp = method.Body.GetILProcessor();
            var instruction = method.Body.Instructions[0];
            var newInstruction = Instruction.Create(OpCodes.Ret); // Return on first line
            ilp.Replace(instruction, newInstruction);
        }

        private void PatchGameVersion(MethodDefinition method)
        {
            var ilp = method.Body.GetILProcessor();
            Instruction instruction = method.Body.Instructions[66];
            Instruction newInstruction = Instruction.Create(OpCodes.Ldc_I4_2);
            ilp.Replace(instruction, newInstruction);
            instruction = method.Body.Instructions[68];
            ilp.Replace(instruction, newInstruction);
        }

        private void RevertModloaderPatch(MethodDefinition method)
        {
            for (int i = method.Body.Instructions.Count - 1; i >= 0; i--)
            {
                var instruction = method.Body.Instructions[i];
                // Logger.Log($"{instruction.OpCode} - {instruction.Operand is MethodReference}");
                if (instruction.OpCode == OpCodes.Call && instruction.Operand is MethodReference methodReference)
                {
                    // Logger.Log($" > {methodReference.Name}");
                    if (methodReference.Name != "LoadModLoader") continue;

                    Logger.Log($"This appears to be a VML modded version of VTOL, removing call to load VML");
                    method.Body.Instructions.RemoveAt(i);
                }
            }
        }


        [DllImport("wintrust.dll", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Unicode)]
        private static extern uint WinVerifyTrust(IntPtr hWnd, IntPtr pgActionID, IntPtr pWinTrustData);

        private static uint WinVerifyTrust(string fileName)
        {

            Guid wintrust_action_generic_verify_v2 = new Guid("{00AAC56B-CD44-11d0-8CC2-00C04FC295EE}");
            uint result = 0;
            using (WINTRUST_FILE_INFO fileInfo = new WINTRUST_FILE_INFO(fileName,
                Guid.Empty))
            using (UnmanagedPointer guidPtr = new UnmanagedPointer(Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Guid))),
                AllocMethod.HGlobal))
            using (UnmanagedPointer wvtDataPtr = new UnmanagedPointer(Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WINTRUST_DATA))),
                AllocMethod.HGlobal))
            {
                WINTRUST_DATA data = new WINTRUST_DATA(fileInfo);
                IntPtr pGuid = guidPtr;
                IntPtr pData = wvtDataPtr;
                Marshal.StructureToPtr(wintrust_action_generic_verify_v2,
                    pGuid,
                    true);
                Marshal.StructureToPtr(data,
                    pData,
                    true);
                result = WinVerifyTrust(IntPtr.Zero,
                    pGuid,
                    pData);

            }
            return result;

        }

        private static bool IsTrusted(string fileName)
        {
            return WinVerifyTrust(fileName) == 0;
        }
    }

    class CustomResolver : BaseAssemblyResolver
    {
        private DefaultAssemblyResolver _defaultResolver;
        private string gameDllsPath;
        public CustomResolver(string path)
        {
            _defaultResolver = new DefaultAssemblyResolver();
            gameDllsPath = path;
        }

        public override AssemblyDefinition Resolve(AssemblyNameReference name)
        {
            AssemblyDefinition assembly;
            try
            {
                assembly = _defaultResolver.Resolve(name);
            }
            catch (AssemblyResolutionException ex)
            {
                // Logger.Log(gameDllsPath + "/" + name.Name + ".dll");
                assembly = AssemblyDefinition.ReadAssembly(gameDllsPath + "/" + name.Name + ".dll");
            }
            return assembly;
        }
    }
}