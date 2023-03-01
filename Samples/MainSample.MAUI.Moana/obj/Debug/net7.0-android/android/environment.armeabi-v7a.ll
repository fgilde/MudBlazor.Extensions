; ModuleID = 'obj\Debug\net7.0-android\android\environment.armeabi-v7a.ll'
source_filename = "obj\Debug\net7.0-android\android\environment.armeabi-v7a.ll"
target datalayout = "e-m:e-p:32:32-Fi8-i64:64-v128:64:128-a:0:32-n32-S64"
target triple = "armv7-unknown-linux-android"


%struct.ApplicationConfig = type {
	i8,; bool uses_mono_llvm
	i8,; bool uses_mono_aot
	i8,; bool aot_lazy_load
	i8,; bool uses_assembly_preload
	i8,; bool is_a_bundled_app
	i8,; bool broken_exception_transitions
	i8,; bool instant_run_enabled
	i8,; bool jni_add_native_method_registration_attribute_present
	i8,; bool have_runtime_config_blob
	i8,; bool have_assemblies_blob
	i8,; uint8_t bound_stream_io_exception_type
	i32,; uint32_t package_naming_policy
	i32,; uint32_t environment_variable_count
	i32,; uint32_t system_property_count
	i32,; uint32_t number_of_assemblies_in_apk
	i32,; uint32_t bundled_assembly_name_width
	i32,; uint32_t number_of_assembly_store_files
	i32,; uint32_t number_of_dso_cache_entries
	i32,; uint32_t android_runtime_jnienv_class_token
	i32,; uint32_t jnienv_initialize_method_token
	i32,; uint32_t jnienv_registerjninatives_method_token
	i32,; uint32_t jni_remapping_replacement_type_count
	i32,; uint32_t jni_remapping_replacement_method_index_entry_count
	i32,; uint32_t mono_components_mask
	i8*; char* android_package_name
}

%struct.AssemblyStoreAssemblyDescriptor = type {
	i32,; uint32_t data_offset
	i32,; uint32_t data_size
	i32,; uint32_t debug_data_offset
	i32,; uint32_t debug_data_size
	i32,; uint32_t config_data_offset
	i32; uint32_t config_data_size
}

%struct.AssemblyStoreSingleAssemblyRuntimeData = type {
	i8*,; uint8_t* image_data
	i8*,; uint8_t* debug_info_data
	i8*,; uint8_t* config_data
	%struct.AssemblyStoreAssemblyDescriptor*; AssemblyStoreAssemblyDescriptor* descriptor
}

%struct.AssemblyStoreRuntimeData = type {
	i8*,; uint8_t* data_start
	i32,; uint32_t assembly_count
	%struct.AssemblyStoreAssemblyDescriptor*; AssemblyStoreAssemblyDescriptor* assemblies
}

%struct.XamarinAndroidBundledAssembly = type {
	i32,; int32_t apk_fd
	i32,; uint32_t data_offset
	i32,; uint32_t data_size
	i8*,; uint8_t* data
	i32,; uint32_t name_length
	i8*; char* name
}

%struct.DSOCacheEntry = type {
	i64,; uint64_t hash
	i8,; bool ignore
	i8*,; char* name
	i8*; void* handle
}

@format_tag = local_unnamed_addr constant i64 385281960275288, align 8
@__mono_aot_mode_name = internal constant [7 x i8] c"interp\00", align 1
@mono_aot_mode_name = local_unnamed_addr constant i8* getelementptr inbounds ([7 x i8], [7 x i8]* @__mono_aot_mode_name, i32 0, i32 0), align 4

; app_environment_variables
@__app_environment_variables_n_0.0 = internal constant [29 x i8] c"DOTNET_MODIFIABLE_ASSEMBLIES\00", align 1
@__app_environment_variables_v_0.1 = internal constant [6 x i8] c"Debug\00", align 1
@__app_environment_variables_n_1.2 = internal constant [15 x i8] c"MONO_GC_PARAMS\00", align 1
@__app_environment_variables_v_1.3 = internal constant [21 x i8] c"major=marksweep-conc\00", align 1
@__app_environment_variables_n_2.4 = internal constant [15 x i8] c"MONO_LOG_LEVEL\00", align 1
@__app_environment_variables_v_2.5 = internal constant [5 x i8] c"info\00", align 1
@__app_environment_variables_n_3.6 = internal constant [17 x i8] c"XAMARIN_BUILD_ID\00", align 1
@__app_environment_variables_v_3.7 = internal constant [37 x i8] c"df08c08f-07d2-4dde-a060-d7d64b9e5489\00", align 1
@__app_environment_variables_n_4.8 = internal constant [28 x i8] c"XA_HTTP_CLIENT_HANDLER_TYPE\00", align 1
@__app_environment_variables_v_4.9 = internal constant [42 x i8] c"Xamarin.Android.Net.AndroidMessageHandler\00", align 1
@__app_environment_variables_n_5.10 = internal constant [29 x i8] c"__XA_PACKAGE_NAMING_POLICY__\00", align 1
@__app_environment_variables_v_5.11 = internal constant [15 x i8] c"LowercaseCrc64\00", align 1

@app_environment_variables = local_unnamed_addr constant [12 x i8*] [
	i8* getelementptr inbounds ([29 x i8], [29 x i8]* @__app_environment_variables_n_0.0, i32 0, i32 0),
	i8* getelementptr inbounds ([6 x i8], [6 x i8]* @__app_environment_variables_v_0.1, i32 0, i32 0),
	i8* getelementptr inbounds ([15 x i8], [15 x i8]* @__app_environment_variables_n_1.2, i32 0, i32 0),
	i8* getelementptr inbounds ([21 x i8], [21 x i8]* @__app_environment_variables_v_1.3, i32 0, i32 0),
	i8* getelementptr inbounds ([15 x i8], [15 x i8]* @__app_environment_variables_n_2.4, i32 0, i32 0),
	i8* getelementptr inbounds ([5 x i8], [5 x i8]* @__app_environment_variables_v_2.5, i32 0, i32 0),
	i8* getelementptr inbounds ([17 x i8], [17 x i8]* @__app_environment_variables_n_3.6, i32 0, i32 0),
	i8* getelementptr inbounds ([37 x i8], [37 x i8]* @__app_environment_variables_v_3.7, i32 0, i32 0),
	i8* getelementptr inbounds ([28 x i8], [28 x i8]* @__app_environment_variables_n_4.8, i32 0, i32 0),
	i8* getelementptr inbounds ([42 x i8], [42 x i8]* @__app_environment_variables_v_4.9, i32 0, i32 0),
	i8* getelementptr inbounds ([29 x i8], [29 x i8]* @__app_environment_variables_n_5.10, i32 0, i32 0),
	i8* getelementptr inbounds ([15 x i8], [15 x i8]* @__app_environment_variables_v_5.11, i32 0, i32 0)
], align 4

; app_system_properties
@app_system_properties = local_unnamed_addr constant [0 x i8*] zeroinitializer, align 4
@__ApplicationConfig_android_package_name.0 = internal constant [22 x i8] c"com.companyname.moana\00", align 1

; application_config
@application_config = local_unnamed_addr constant %struct.ApplicationConfig {
	i8 0, ; uses_mono_llvm
	i8 1, ; uses_mono_aot
	i8 0, ; aot_lazy_load
	i8 0, ; uses_assembly_preload
	i8 0, ; is_a_bundled_app
	i8 0, ; broken_exception_transitions
	i8 0, ; instant_run_enabled
	i8 0, ; jni_add_native_method_registration_attribute_present
	i8 1, ; have_runtime_config_blob
	i8 0, ; have_assemblies_blob
	i8 0, ; bound_stream_io_exception_type
	i32 3, ; package_naming_policy
	i32 12, ; environment_variable_count
	i32 0, ; system_property_count
	i32 379, ; number_of_assemblies_in_apk
	i32 70, ; bundled_assembly_name_width
	i32 2, ; number_of_assembly_store_files
	i32 32, ; number_of_dso_cache_entries
	i32 33560110, ; android_runtime_jnienv_class_token
	i32 100762566, ; jnienv_initialize_method_token
	i32 100762565, ; jnienv_registerjninatives_method_token
	i32 0, ; jni_remapping_replacement_type_count
	i32 0, ; jni_remapping_replacement_method_index_entry_count
	i32 3, ; mono_components_mask
	i8* getelementptr inbounds ([22 x i8], [22 x i8]* @__ApplicationConfig_android_package_name.0, i32 0, i32 0); android_package_name
}, align 4

@__DSOCacheEntry_name.1 = internal constant [31 x i8] c"libxamarin-debug-app-helper.so\00", align 1
@__DSOCacheEntry_name.2 = internal constant [50 x i8] c"libSystem.Security.Cryptography.Native.Android.so\00", align 1
@__DSOCacheEntry_name.3 = internal constant [16 x i8] c"libmonodroid.so\00", align 1
@__DSOCacheEntry_name.4 = internal constant [30 x i8] c"libmono-component-debugger.so\00", align 1
@__DSOCacheEntry_name.5 = internal constant [20 x i8] c"libSystem.Native.so\00", align 1
@__DSOCacheEntry_name.6 = internal constant [32 x i8] c"libmono-component-hot_reload.so\00", align 1
@__DSOCacheEntry_name.7 = internal constant [35 x i8] c"libSystem.IO.Compression.Native.so\00", align 1
@__DSOCacheEntry_name.8 = internal constant [19 x i8] c"libmonosgen-2.0.so\00", align 1

; dso_cache
@dso_cache = local_unnamed_addr global [32 x %struct.DSOCacheEntry] [
	; 0
	%struct.DSOCacheEntry {
		i64 98567696, ; hash 0x5e00610, from name: libxamarin-debug-app-helper.so
		i8 0, ; ignore
		i8* getelementptr inbounds ([31 x i8], [31 x i8]* @__DSOCacheEntry_name.1, i32 0, i32 0), ; name
		i8* null; handle
	}, 
	; 1
	%struct.DSOCacheEntry {
		i64 154543565, ; hash 0x93625cd, from name: libSystem.Security.Cryptography.Native.Android
		i8 0, ; ignore
		i8* getelementptr inbounds ([50 x i8], [50 x i8]* @__DSOCacheEntry_name.2, i32 0, i32 0), ; name
		i8* null; handle
	}, 
	; 2
	%struct.DSOCacheEntry {
		i64 229294244, ; hash 0xdaac0a4, from name: monodroid.so
		i8 0, ; ignore
		i8* getelementptr inbounds ([16 x i8], [16 x i8]* @__DSOCacheEntry_name.3, i32 0, i32 0), ; name
		i8* null; handle
	}, 
	; 3
	%struct.DSOCacheEntry {
		i64 289063585, ; hash 0x113ac2a1, from name: libmono-component-debugger
		i8 0, ; ignore
		i8* getelementptr inbounds ([30 x i8], [30 x i8]* @__DSOCacheEntry_name.4, i32 0, i32 0), ; name
		i8* null; handle
	}, 
	; 4
	%struct.DSOCacheEntry {
		i64 500609955, ; hash 0x1dd6b3a3, from name: System.Native.so
		i8 0, ; ignore
		i8* getelementptr inbounds ([20 x i8], [20 x i8]* @__DSOCacheEntry_name.5, i32 0, i32 0), ; name
		i8* null; handle
	}, 
	; 5
	%struct.DSOCacheEntry {
		i64 713151617, ; hash 0x2a81d481, from name: libxamarin-debug-app-helper
		i8 0, ; ignore
		i8* getelementptr inbounds ([31 x i8], [31 x i8]* @__DSOCacheEntry_name.1, i32 0, i32 0), ; name
		i8* null; handle
	}, 
	; 6
	%struct.DSOCacheEntry {
		i64 748366034, ; hash 0x2c9b28d2, from name: monodroid
		i8 0, ; ignore
		i8* getelementptr inbounds ([16 x i8], [16 x i8]* @__DSOCacheEntry_name.3, i32 0, i32 0), ; name
		i8* null; handle
	}, 
	; 7
	%struct.DSOCacheEntry {
		i64 808873553, ; hash 0x30366e51, from name: libmono-component-hot_reload.so
		i8 0, ; ignore
		i8* getelementptr inbounds ([32 x i8], [32 x i8]* @__DSOCacheEntry_name.6, i32 0, i32 0), ; name
		i8* null; handle
	}, 
	; 8
	%struct.DSOCacheEntry {
		i64 870587408, ; hash 0x33e41c10, from name: System.Security.Cryptography.Native.Android.so
		i8 0, ; ignore
		i8* getelementptr inbounds ([50 x i8], [50 x i8]* @__DSOCacheEntry_name.2, i32 0, i32 0), ; name
		i8* null; handle
	}, 
	; 9
	%struct.DSOCacheEntry {
		i64 1358324080, ; hash 0x50f66170, from name: mono-component-hot_reload
		i8 0, ; ignore
		i8* getelementptr inbounds ([32 x i8], [32 x i8]* @__DSOCacheEntry_name.6, i32 0, i32 0), ; name
		i8* null; handle
	}, 
	; 10
	%struct.DSOCacheEntry {
		i64 1398864029, ; hash 0x5360f89d, from name: System.Security.Cryptography.Native.Android
		i8 0, ; ignore
		i8* getelementptr inbounds ([50 x i8], [50 x i8]* @__DSOCacheEntry_name.2, i32 0, i32 0), ; name
		i8* null; handle
	}, 
	; 11
	%struct.DSOCacheEntry {
		i64 1516058787, ; hash 0x5a5d38a3, from name: xamarin-debug-app-helper.so
		i8 0, ; ignore
		i8* getelementptr inbounds ([31 x i8], [31 x i8]* @__DSOCacheEntry_name.1, i32 0, i32 0), ; name
		i8* null; handle
	}, 
	; 12
	%struct.DSOCacheEntry {
		i64 1536876128, ; hash 0x5b9ade60, from name: libSystem.Native
		i8 0, ; ignore
		i8* getelementptr inbounds ([20 x i8], [20 x i8]* @__DSOCacheEntry_name.5, i32 0, i32 0), ; name
		i8* null; handle
	}, 
	; 13
	%struct.DSOCacheEntry {
		i64 1959705688, ; hash 0x74cebc58, from name: System.IO.Compression.Native
		i8 0, ; ignore
		i8* getelementptr inbounds ([35 x i8], [35 x i8]* @__DSOCacheEntry_name.7, i32 0, i32 0), ; name
		i8* null; handle
	}, 
	; 14
	%struct.DSOCacheEntry {
		i64 2044108986, ; hash 0x79d6a0ba, from name: libSystem.Native.so
		i8 0, ; ignore
		i8* getelementptr inbounds ([20 x i8], [20 x i8]* @__DSOCacheEntry_name.5, i32 0, i32 0), ; name
		i8* null; handle
	}, 
	; 15
	%struct.DSOCacheEntry {
		i64 2072777569, ; hash 0x7b8c1361, from name: System.IO.Compression.Native.so
		i8 0, ; ignore
		i8* getelementptr inbounds ([35 x i8], [35 x i8]* @__DSOCacheEntry_name.7, i32 0, i32 0), ; name
		i8* null; handle
	}, 
	; 16
	%struct.DSOCacheEntry {
		i64 2101192894, ; hash 0x7d3da8be, from name: libSystem.Security.Cryptography.Native.Android.so
		i8 0, ; ignore
		i8* getelementptr inbounds ([50 x i8], [50 x i8]* @__DSOCacheEntry_name.2, i32 0, i32 0), ; name
		i8* null; handle
	}, 
	; 17
	%struct.DSOCacheEntry {
		i64 2229681966, ; hash 0x84e63f2e, from name: xamarin-debug-app-helper
		i8 0, ; ignore
		i8* getelementptr inbounds ([31 x i8], [31 x i8]* @__DSOCacheEntry_name.1, i32 0, i32 0), ; name
		i8* null; handle
	}, 
	; 18
	%struct.DSOCacheEntry {
		i64 2496112763, ; hash 0x94c7a87b, from name: libmonosgen-2.0
		i8 0, ; ignore
		i8* getelementptr inbounds ([19 x i8], [19 x i8]* @__DSOCacheEntry_name.8, i32 0, i32 0), ; name
		i8* null; handle
	}, 
	; 19
	%struct.DSOCacheEntry {
		i64 2578174356, ; hash 0x99abd194, from name: System.Native
		i8 0, ; ignore
		i8* getelementptr inbounds ([20 x i8], [20 x i8]* @__DSOCacheEntry_name.5, i32 0, i32 0), ; name
		i8* null; handle
	}, 
	; 20
	%struct.DSOCacheEntry {
		i64 2658598962, ; hash 0x9e770032, from name: monosgen-2.0.so
		i8 0, ; ignore
		i8* getelementptr inbounds ([19 x i8], [19 x i8]* @__DSOCacheEntry_name.8, i32 0, i32 0), ; name
		i8* null; handle
	}, 
	; 21
	%struct.DSOCacheEntry {
		i64 2938740861, ; hash 0xaf29a07d, from name: libSystem.IO.Compression.Native.so
		i8 0, ; ignore
		i8* getelementptr inbounds ([35 x i8], [35 x i8]* @__DSOCacheEntry_name.7, i32 0, i32 0), ; name
		i8* null; handle
	}, 
	; 22
	%struct.DSOCacheEntry {
		i64 2950894636, ; hash 0xafe3142c, from name: libSystem.IO.Compression.Native
		i8 0, ; ignore
		i8* getelementptr inbounds ([35 x i8], [35 x i8]* @__DSOCacheEntry_name.7, i32 0, i32 0), ; name
		i8* null; handle
	}, 
	; 23
	%struct.DSOCacheEntry {
		i64 3185845700, ; hash 0xbde425c4, from name: libmono-component-hot_reload
		i8 0, ; ignore
		i8* getelementptr inbounds ([32 x i8], [32 x i8]* @__DSOCacheEntry_name.6, i32 0, i32 0), ; name
		i8* null; handle
	}, 
	; 24
	%struct.DSOCacheEntry {
		i64 3422266863, ; hash 0xcbfba5ef, from name: libmonodroid.so
		i8 0, ; ignore
		i8* getelementptr inbounds ([16 x i8], [16 x i8]* @__DSOCacheEntry_name.3, i32 0, i32 0), ; name
		i8* null; handle
	}, 
	; 25
	%struct.DSOCacheEntry {
		i64 3636393175, ; hash 0xd8bef4d7, from name: libmonodroid
		i8 0, ; ignore
		i8* getelementptr inbounds ([16 x i8], [16 x i8]* @__DSOCacheEntry_name.3, i32 0, i32 0), ; name
		i8* null; handle
	}, 
	; 26
	%struct.DSOCacheEntry {
		i64 3732899189, ; hash 0xde7f8575, from name: mono-component-hot_reload.so
		i8 0, ; ignore
		i8* getelementptr inbounds ([32 x i8], [32 x i8]* @__DSOCacheEntry_name.6, i32 0, i32 0), ; name
		i8* null; handle
	}, 
	; 27
	%struct.DSOCacheEntry {
		i64 3790421216, ; hash 0xe1ed3ce0, from name: monosgen-2.0
		i8 0, ; ignore
		i8* getelementptr inbounds ([19 x i8], [19 x i8]* @__DSOCacheEntry_name.8, i32 0, i32 0), ; name
		i8* null; handle
	}, 
	; 28
	%struct.DSOCacheEntry {
		i64 3817984437, ; hash 0xe391d1b5, from name: libmonosgen-2.0.so
		i8 0, ; ignore
		i8* getelementptr inbounds ([19 x i8], [19 x i8]* @__DSOCacheEntry_name.8, i32 0, i32 0), ; name
		i8* null; handle
	}, 
	; 29
	%struct.DSOCacheEntry {
		i64 4152357740, ; hash 0xf77ff36c, from name: libmono-component-debugger.so
		i8 0, ; ignore
		i8* getelementptr inbounds ([30 x i8], [30 x i8]* @__DSOCacheEntry_name.4, i32 0, i32 0), ; name
		i8* null; handle
	}, 
	; 30
	%struct.DSOCacheEntry {
		i64 4184302796, ; hash 0xf96764cc, from name: mono-component-debugger.so
		i8 0, ; ignore
		i8* getelementptr inbounds ([30 x i8], [30 x i8]* @__DSOCacheEntry_name.4, i32 0, i32 0), ; name
		i8* null; handle
	}, 
	; 31
	%struct.DSOCacheEntry {
		i64 4288261976, ; hash 0xff99af58, from name: mono-component-debugger
		i8 0, ; ignore
		i8* getelementptr inbounds ([30 x i8], [30 x i8]* @__DSOCacheEntry_name.4, i32 0, i32 0), ; name
		i8* null; handle
	}
], align 8; end of 'dso_cache' array

@__XamarinAndroidBundledAssembly_name_0 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_1 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_2 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_3 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_4 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_5 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_6 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_7 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_8 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_9 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_10 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_11 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_12 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_13 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_14 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_15 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_16 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_17 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_18 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_19 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_20 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_21 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_22 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_23 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_24 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_25 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_26 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_27 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_28 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_29 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_30 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_31 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_32 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_33 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_34 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_35 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_36 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_37 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_38 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_39 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_40 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_41 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_42 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_43 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_44 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_45 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_46 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_47 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_48 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_49 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_50 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_51 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_52 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_53 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_54 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_55 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_56 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_57 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_58 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_59 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_60 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_61 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_62 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_63 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_64 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_65 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_66 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_67 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_68 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_69 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_70 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_71 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_72 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_73 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_74 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_75 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_76 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_77 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_78 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_79 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_80 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_81 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_82 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_83 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_84 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_85 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_86 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_87 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_88 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_89 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_90 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_91 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_92 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_93 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_94 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_95 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_96 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_97 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_98 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_99 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_100 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_101 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_102 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_103 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_104 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_105 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_106 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_107 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_108 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_109 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_110 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_111 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_112 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_113 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_114 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_115 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_116 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_117 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_118 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_119 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_120 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_121 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_122 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_123 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_124 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_125 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_126 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_127 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_128 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_129 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_130 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_131 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_132 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_133 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_134 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_135 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_136 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_137 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_138 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_139 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_140 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_141 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_142 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_143 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_144 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_145 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_146 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_147 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_148 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_149 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_150 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_151 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_152 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_153 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_154 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_155 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_156 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_157 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_158 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_159 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_160 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_161 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_162 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_163 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_164 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_165 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_166 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_167 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_168 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_169 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_170 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_171 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_172 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_173 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_174 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_175 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_176 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_177 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_178 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_179 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_180 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_181 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_182 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_183 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_184 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_185 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_186 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_187 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_188 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_189 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_190 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_191 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_192 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_193 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_194 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_195 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_196 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_197 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_198 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_199 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_200 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_201 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_202 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_203 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_204 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_205 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_206 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_207 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_208 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_209 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_210 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_211 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_212 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_213 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_214 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_215 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_216 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_217 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_218 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_219 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_220 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_221 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_222 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_223 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_224 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_225 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_226 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_227 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_228 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_229 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_230 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_231 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_232 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_233 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_234 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_235 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_236 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_237 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_238 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_239 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_240 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_241 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_242 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_243 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_244 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_245 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_246 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_247 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_248 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_249 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_250 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_251 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_252 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_253 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_254 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_255 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_256 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_257 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_258 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_259 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_260 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_261 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_262 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_263 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_264 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_265 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_266 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_267 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_268 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_269 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_270 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_271 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_272 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_273 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_274 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_275 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_276 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_277 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_278 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_279 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_280 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_281 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_282 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_283 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_284 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_285 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_286 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_287 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_288 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_289 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_290 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_291 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_292 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_293 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_294 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_295 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_296 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_297 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_298 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_299 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_300 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_301 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_302 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_303 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_304 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_305 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_306 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_307 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_308 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_309 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_310 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_311 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_312 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_313 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_314 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_315 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_316 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_317 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_318 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_319 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_320 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_321 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_322 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_323 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_324 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_325 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_326 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_327 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_328 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_329 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_330 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_331 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_332 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_333 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_334 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_335 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_336 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_337 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_338 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_339 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_340 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_341 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_342 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_343 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_344 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_345 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_346 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_347 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_348 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_349 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_350 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_351 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_352 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_353 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_354 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_355 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_356 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_357 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_358 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_359 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_360 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_361 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_362 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_363 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_364 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_365 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_366 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_367 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_368 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_369 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_370 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_371 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_372 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_373 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_374 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_375 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_376 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_377 = internal global [70 x i8] zeroinitializer, align 1
@__XamarinAndroidBundledAssembly_name_378 = internal global [70 x i8] zeroinitializer, align 1


; Bundled assembly name buffers, all 70 bytes long
@bundled_assemblies = local_unnamed_addr global [379 x %struct.XamarinAndroidBundledAssembly] [
	; 0
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_0, i32 0, i32 0); name
	}, 
	; 1
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_1, i32 0, i32 0); name
	}, 
	; 2
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_2, i32 0, i32 0); name
	}, 
	; 3
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_3, i32 0, i32 0); name
	}, 
	; 4
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_4, i32 0, i32 0); name
	}, 
	; 5
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_5, i32 0, i32 0); name
	}, 
	; 6
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_6, i32 0, i32 0); name
	}, 
	; 7
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_7, i32 0, i32 0); name
	}, 
	; 8
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_8, i32 0, i32 0); name
	}, 
	; 9
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_9, i32 0, i32 0); name
	}, 
	; 10
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_10, i32 0, i32 0); name
	}, 
	; 11
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_11, i32 0, i32 0); name
	}, 
	; 12
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_12, i32 0, i32 0); name
	}, 
	; 13
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_13, i32 0, i32 0); name
	}, 
	; 14
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_14, i32 0, i32 0); name
	}, 
	; 15
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_15, i32 0, i32 0); name
	}, 
	; 16
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_16, i32 0, i32 0); name
	}, 
	; 17
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_17, i32 0, i32 0); name
	}, 
	; 18
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_18, i32 0, i32 0); name
	}, 
	; 19
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_19, i32 0, i32 0); name
	}, 
	; 20
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_20, i32 0, i32 0); name
	}, 
	; 21
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_21, i32 0, i32 0); name
	}, 
	; 22
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_22, i32 0, i32 0); name
	}, 
	; 23
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_23, i32 0, i32 0); name
	}, 
	; 24
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_24, i32 0, i32 0); name
	}, 
	; 25
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_25, i32 0, i32 0); name
	}, 
	; 26
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_26, i32 0, i32 0); name
	}, 
	; 27
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_27, i32 0, i32 0); name
	}, 
	; 28
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_28, i32 0, i32 0); name
	}, 
	; 29
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_29, i32 0, i32 0); name
	}, 
	; 30
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_30, i32 0, i32 0); name
	}, 
	; 31
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_31, i32 0, i32 0); name
	}, 
	; 32
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_32, i32 0, i32 0); name
	}, 
	; 33
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_33, i32 0, i32 0); name
	}, 
	; 34
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_34, i32 0, i32 0); name
	}, 
	; 35
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_35, i32 0, i32 0); name
	}, 
	; 36
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_36, i32 0, i32 0); name
	}, 
	; 37
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_37, i32 0, i32 0); name
	}, 
	; 38
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_38, i32 0, i32 0); name
	}, 
	; 39
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_39, i32 0, i32 0); name
	}, 
	; 40
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_40, i32 0, i32 0); name
	}, 
	; 41
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_41, i32 0, i32 0); name
	}, 
	; 42
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_42, i32 0, i32 0); name
	}, 
	; 43
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_43, i32 0, i32 0); name
	}, 
	; 44
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_44, i32 0, i32 0); name
	}, 
	; 45
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_45, i32 0, i32 0); name
	}, 
	; 46
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_46, i32 0, i32 0); name
	}, 
	; 47
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_47, i32 0, i32 0); name
	}, 
	; 48
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_48, i32 0, i32 0); name
	}, 
	; 49
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_49, i32 0, i32 0); name
	}, 
	; 50
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_50, i32 0, i32 0); name
	}, 
	; 51
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_51, i32 0, i32 0); name
	}, 
	; 52
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_52, i32 0, i32 0); name
	}, 
	; 53
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_53, i32 0, i32 0); name
	}, 
	; 54
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_54, i32 0, i32 0); name
	}, 
	; 55
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_55, i32 0, i32 0); name
	}, 
	; 56
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_56, i32 0, i32 0); name
	}, 
	; 57
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_57, i32 0, i32 0); name
	}, 
	; 58
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_58, i32 0, i32 0); name
	}, 
	; 59
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_59, i32 0, i32 0); name
	}, 
	; 60
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_60, i32 0, i32 0); name
	}, 
	; 61
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_61, i32 0, i32 0); name
	}, 
	; 62
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_62, i32 0, i32 0); name
	}, 
	; 63
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_63, i32 0, i32 0); name
	}, 
	; 64
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_64, i32 0, i32 0); name
	}, 
	; 65
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_65, i32 0, i32 0); name
	}, 
	; 66
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_66, i32 0, i32 0); name
	}, 
	; 67
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_67, i32 0, i32 0); name
	}, 
	; 68
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_68, i32 0, i32 0); name
	}, 
	; 69
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_69, i32 0, i32 0); name
	}, 
	; 70
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_70, i32 0, i32 0); name
	}, 
	; 71
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_71, i32 0, i32 0); name
	}, 
	; 72
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_72, i32 0, i32 0); name
	}, 
	; 73
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_73, i32 0, i32 0); name
	}, 
	; 74
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_74, i32 0, i32 0); name
	}, 
	; 75
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_75, i32 0, i32 0); name
	}, 
	; 76
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_76, i32 0, i32 0); name
	}, 
	; 77
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_77, i32 0, i32 0); name
	}, 
	; 78
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_78, i32 0, i32 0); name
	}, 
	; 79
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_79, i32 0, i32 0); name
	}, 
	; 80
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_80, i32 0, i32 0); name
	}, 
	; 81
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_81, i32 0, i32 0); name
	}, 
	; 82
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_82, i32 0, i32 0); name
	}, 
	; 83
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_83, i32 0, i32 0); name
	}, 
	; 84
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_84, i32 0, i32 0); name
	}, 
	; 85
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_85, i32 0, i32 0); name
	}, 
	; 86
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_86, i32 0, i32 0); name
	}, 
	; 87
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_87, i32 0, i32 0); name
	}, 
	; 88
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_88, i32 0, i32 0); name
	}, 
	; 89
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_89, i32 0, i32 0); name
	}, 
	; 90
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_90, i32 0, i32 0); name
	}, 
	; 91
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_91, i32 0, i32 0); name
	}, 
	; 92
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_92, i32 0, i32 0); name
	}, 
	; 93
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_93, i32 0, i32 0); name
	}, 
	; 94
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_94, i32 0, i32 0); name
	}, 
	; 95
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_95, i32 0, i32 0); name
	}, 
	; 96
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_96, i32 0, i32 0); name
	}, 
	; 97
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_97, i32 0, i32 0); name
	}, 
	; 98
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_98, i32 0, i32 0); name
	}, 
	; 99
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_99, i32 0, i32 0); name
	}, 
	; 100
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_100, i32 0, i32 0); name
	}, 
	; 101
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_101, i32 0, i32 0); name
	}, 
	; 102
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_102, i32 0, i32 0); name
	}, 
	; 103
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_103, i32 0, i32 0); name
	}, 
	; 104
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_104, i32 0, i32 0); name
	}, 
	; 105
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_105, i32 0, i32 0); name
	}, 
	; 106
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_106, i32 0, i32 0); name
	}, 
	; 107
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_107, i32 0, i32 0); name
	}, 
	; 108
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_108, i32 0, i32 0); name
	}, 
	; 109
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_109, i32 0, i32 0); name
	}, 
	; 110
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_110, i32 0, i32 0); name
	}, 
	; 111
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_111, i32 0, i32 0); name
	}, 
	; 112
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_112, i32 0, i32 0); name
	}, 
	; 113
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_113, i32 0, i32 0); name
	}, 
	; 114
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_114, i32 0, i32 0); name
	}, 
	; 115
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_115, i32 0, i32 0); name
	}, 
	; 116
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_116, i32 0, i32 0); name
	}, 
	; 117
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_117, i32 0, i32 0); name
	}, 
	; 118
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_118, i32 0, i32 0); name
	}, 
	; 119
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_119, i32 0, i32 0); name
	}, 
	; 120
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_120, i32 0, i32 0); name
	}, 
	; 121
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_121, i32 0, i32 0); name
	}, 
	; 122
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_122, i32 0, i32 0); name
	}, 
	; 123
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_123, i32 0, i32 0); name
	}, 
	; 124
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_124, i32 0, i32 0); name
	}, 
	; 125
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_125, i32 0, i32 0); name
	}, 
	; 126
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_126, i32 0, i32 0); name
	}, 
	; 127
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_127, i32 0, i32 0); name
	}, 
	; 128
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_128, i32 0, i32 0); name
	}, 
	; 129
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_129, i32 0, i32 0); name
	}, 
	; 130
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_130, i32 0, i32 0); name
	}, 
	; 131
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_131, i32 0, i32 0); name
	}, 
	; 132
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_132, i32 0, i32 0); name
	}, 
	; 133
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_133, i32 0, i32 0); name
	}, 
	; 134
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_134, i32 0, i32 0); name
	}, 
	; 135
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_135, i32 0, i32 0); name
	}, 
	; 136
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_136, i32 0, i32 0); name
	}, 
	; 137
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_137, i32 0, i32 0); name
	}, 
	; 138
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_138, i32 0, i32 0); name
	}, 
	; 139
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_139, i32 0, i32 0); name
	}, 
	; 140
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_140, i32 0, i32 0); name
	}, 
	; 141
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_141, i32 0, i32 0); name
	}, 
	; 142
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_142, i32 0, i32 0); name
	}, 
	; 143
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_143, i32 0, i32 0); name
	}, 
	; 144
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_144, i32 0, i32 0); name
	}, 
	; 145
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_145, i32 0, i32 0); name
	}, 
	; 146
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_146, i32 0, i32 0); name
	}, 
	; 147
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_147, i32 0, i32 0); name
	}, 
	; 148
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_148, i32 0, i32 0); name
	}, 
	; 149
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_149, i32 0, i32 0); name
	}, 
	; 150
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_150, i32 0, i32 0); name
	}, 
	; 151
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_151, i32 0, i32 0); name
	}, 
	; 152
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_152, i32 0, i32 0); name
	}, 
	; 153
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_153, i32 0, i32 0); name
	}, 
	; 154
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_154, i32 0, i32 0); name
	}, 
	; 155
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_155, i32 0, i32 0); name
	}, 
	; 156
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_156, i32 0, i32 0); name
	}, 
	; 157
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_157, i32 0, i32 0); name
	}, 
	; 158
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_158, i32 0, i32 0); name
	}, 
	; 159
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_159, i32 0, i32 0); name
	}, 
	; 160
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_160, i32 0, i32 0); name
	}, 
	; 161
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_161, i32 0, i32 0); name
	}, 
	; 162
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_162, i32 0, i32 0); name
	}, 
	; 163
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_163, i32 0, i32 0); name
	}, 
	; 164
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_164, i32 0, i32 0); name
	}, 
	; 165
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_165, i32 0, i32 0); name
	}, 
	; 166
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_166, i32 0, i32 0); name
	}, 
	; 167
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_167, i32 0, i32 0); name
	}, 
	; 168
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_168, i32 0, i32 0); name
	}, 
	; 169
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_169, i32 0, i32 0); name
	}, 
	; 170
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_170, i32 0, i32 0); name
	}, 
	; 171
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_171, i32 0, i32 0); name
	}, 
	; 172
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_172, i32 0, i32 0); name
	}, 
	; 173
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_173, i32 0, i32 0); name
	}, 
	; 174
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_174, i32 0, i32 0); name
	}, 
	; 175
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_175, i32 0, i32 0); name
	}, 
	; 176
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_176, i32 0, i32 0); name
	}, 
	; 177
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_177, i32 0, i32 0); name
	}, 
	; 178
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_178, i32 0, i32 0); name
	}, 
	; 179
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_179, i32 0, i32 0); name
	}, 
	; 180
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_180, i32 0, i32 0); name
	}, 
	; 181
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_181, i32 0, i32 0); name
	}, 
	; 182
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_182, i32 0, i32 0); name
	}, 
	; 183
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_183, i32 0, i32 0); name
	}, 
	; 184
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_184, i32 0, i32 0); name
	}, 
	; 185
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_185, i32 0, i32 0); name
	}, 
	; 186
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_186, i32 0, i32 0); name
	}, 
	; 187
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_187, i32 0, i32 0); name
	}, 
	; 188
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_188, i32 0, i32 0); name
	}, 
	; 189
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_189, i32 0, i32 0); name
	}, 
	; 190
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_190, i32 0, i32 0); name
	}, 
	; 191
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_191, i32 0, i32 0); name
	}, 
	; 192
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_192, i32 0, i32 0); name
	}, 
	; 193
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_193, i32 0, i32 0); name
	}, 
	; 194
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_194, i32 0, i32 0); name
	}, 
	; 195
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_195, i32 0, i32 0); name
	}, 
	; 196
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_196, i32 0, i32 0); name
	}, 
	; 197
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_197, i32 0, i32 0); name
	}, 
	; 198
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_198, i32 0, i32 0); name
	}, 
	; 199
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_199, i32 0, i32 0); name
	}, 
	; 200
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_200, i32 0, i32 0); name
	}, 
	; 201
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_201, i32 0, i32 0); name
	}, 
	; 202
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_202, i32 0, i32 0); name
	}, 
	; 203
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_203, i32 0, i32 0); name
	}, 
	; 204
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_204, i32 0, i32 0); name
	}, 
	; 205
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_205, i32 0, i32 0); name
	}, 
	; 206
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_206, i32 0, i32 0); name
	}, 
	; 207
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_207, i32 0, i32 0); name
	}, 
	; 208
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_208, i32 0, i32 0); name
	}, 
	; 209
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_209, i32 0, i32 0); name
	}, 
	; 210
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_210, i32 0, i32 0); name
	}, 
	; 211
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_211, i32 0, i32 0); name
	}, 
	; 212
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_212, i32 0, i32 0); name
	}, 
	; 213
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_213, i32 0, i32 0); name
	}, 
	; 214
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_214, i32 0, i32 0); name
	}, 
	; 215
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_215, i32 0, i32 0); name
	}, 
	; 216
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_216, i32 0, i32 0); name
	}, 
	; 217
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_217, i32 0, i32 0); name
	}, 
	; 218
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_218, i32 0, i32 0); name
	}, 
	; 219
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_219, i32 0, i32 0); name
	}, 
	; 220
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_220, i32 0, i32 0); name
	}, 
	; 221
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_221, i32 0, i32 0); name
	}, 
	; 222
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_222, i32 0, i32 0); name
	}, 
	; 223
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_223, i32 0, i32 0); name
	}, 
	; 224
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_224, i32 0, i32 0); name
	}, 
	; 225
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_225, i32 0, i32 0); name
	}, 
	; 226
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_226, i32 0, i32 0); name
	}, 
	; 227
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_227, i32 0, i32 0); name
	}, 
	; 228
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_228, i32 0, i32 0); name
	}, 
	; 229
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_229, i32 0, i32 0); name
	}, 
	; 230
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_230, i32 0, i32 0); name
	}, 
	; 231
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_231, i32 0, i32 0); name
	}, 
	; 232
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_232, i32 0, i32 0); name
	}, 
	; 233
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_233, i32 0, i32 0); name
	}, 
	; 234
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_234, i32 0, i32 0); name
	}, 
	; 235
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_235, i32 0, i32 0); name
	}, 
	; 236
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_236, i32 0, i32 0); name
	}, 
	; 237
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_237, i32 0, i32 0); name
	}, 
	; 238
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_238, i32 0, i32 0); name
	}, 
	; 239
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_239, i32 0, i32 0); name
	}, 
	; 240
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_240, i32 0, i32 0); name
	}, 
	; 241
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_241, i32 0, i32 0); name
	}, 
	; 242
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_242, i32 0, i32 0); name
	}, 
	; 243
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_243, i32 0, i32 0); name
	}, 
	; 244
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_244, i32 0, i32 0); name
	}, 
	; 245
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_245, i32 0, i32 0); name
	}, 
	; 246
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_246, i32 0, i32 0); name
	}, 
	; 247
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_247, i32 0, i32 0); name
	}, 
	; 248
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_248, i32 0, i32 0); name
	}, 
	; 249
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_249, i32 0, i32 0); name
	}, 
	; 250
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_250, i32 0, i32 0); name
	}, 
	; 251
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_251, i32 0, i32 0); name
	}, 
	; 252
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_252, i32 0, i32 0); name
	}, 
	; 253
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_253, i32 0, i32 0); name
	}, 
	; 254
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_254, i32 0, i32 0); name
	}, 
	; 255
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_255, i32 0, i32 0); name
	}, 
	; 256
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_256, i32 0, i32 0); name
	}, 
	; 257
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_257, i32 0, i32 0); name
	}, 
	; 258
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_258, i32 0, i32 0); name
	}, 
	; 259
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_259, i32 0, i32 0); name
	}, 
	; 260
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_260, i32 0, i32 0); name
	}, 
	; 261
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_261, i32 0, i32 0); name
	}, 
	; 262
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_262, i32 0, i32 0); name
	}, 
	; 263
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_263, i32 0, i32 0); name
	}, 
	; 264
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_264, i32 0, i32 0); name
	}, 
	; 265
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_265, i32 0, i32 0); name
	}, 
	; 266
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_266, i32 0, i32 0); name
	}, 
	; 267
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_267, i32 0, i32 0); name
	}, 
	; 268
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_268, i32 0, i32 0); name
	}, 
	; 269
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_269, i32 0, i32 0); name
	}, 
	; 270
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_270, i32 0, i32 0); name
	}, 
	; 271
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_271, i32 0, i32 0); name
	}, 
	; 272
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_272, i32 0, i32 0); name
	}, 
	; 273
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_273, i32 0, i32 0); name
	}, 
	; 274
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_274, i32 0, i32 0); name
	}, 
	; 275
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_275, i32 0, i32 0); name
	}, 
	; 276
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_276, i32 0, i32 0); name
	}, 
	; 277
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_277, i32 0, i32 0); name
	}, 
	; 278
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_278, i32 0, i32 0); name
	}, 
	; 279
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_279, i32 0, i32 0); name
	}, 
	; 280
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_280, i32 0, i32 0); name
	}, 
	; 281
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_281, i32 0, i32 0); name
	}, 
	; 282
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_282, i32 0, i32 0); name
	}, 
	; 283
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_283, i32 0, i32 0); name
	}, 
	; 284
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_284, i32 0, i32 0); name
	}, 
	; 285
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_285, i32 0, i32 0); name
	}, 
	; 286
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_286, i32 0, i32 0); name
	}, 
	; 287
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_287, i32 0, i32 0); name
	}, 
	; 288
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_288, i32 0, i32 0); name
	}, 
	; 289
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_289, i32 0, i32 0); name
	}, 
	; 290
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_290, i32 0, i32 0); name
	}, 
	; 291
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_291, i32 0, i32 0); name
	}, 
	; 292
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_292, i32 0, i32 0); name
	}, 
	; 293
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_293, i32 0, i32 0); name
	}, 
	; 294
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_294, i32 0, i32 0); name
	}, 
	; 295
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_295, i32 0, i32 0); name
	}, 
	; 296
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_296, i32 0, i32 0); name
	}, 
	; 297
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_297, i32 0, i32 0); name
	}, 
	; 298
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_298, i32 0, i32 0); name
	}, 
	; 299
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_299, i32 0, i32 0); name
	}, 
	; 300
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_300, i32 0, i32 0); name
	}, 
	; 301
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_301, i32 0, i32 0); name
	}, 
	; 302
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_302, i32 0, i32 0); name
	}, 
	; 303
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_303, i32 0, i32 0); name
	}, 
	; 304
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_304, i32 0, i32 0); name
	}, 
	; 305
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_305, i32 0, i32 0); name
	}, 
	; 306
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_306, i32 0, i32 0); name
	}, 
	; 307
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_307, i32 0, i32 0); name
	}, 
	; 308
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_308, i32 0, i32 0); name
	}, 
	; 309
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_309, i32 0, i32 0); name
	}, 
	; 310
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_310, i32 0, i32 0); name
	}, 
	; 311
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_311, i32 0, i32 0); name
	}, 
	; 312
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_312, i32 0, i32 0); name
	}, 
	; 313
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_313, i32 0, i32 0); name
	}, 
	; 314
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_314, i32 0, i32 0); name
	}, 
	; 315
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_315, i32 0, i32 0); name
	}, 
	; 316
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_316, i32 0, i32 0); name
	}, 
	; 317
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_317, i32 0, i32 0); name
	}, 
	; 318
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_318, i32 0, i32 0); name
	}, 
	; 319
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_319, i32 0, i32 0); name
	}, 
	; 320
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_320, i32 0, i32 0); name
	}, 
	; 321
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_321, i32 0, i32 0); name
	}, 
	; 322
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_322, i32 0, i32 0); name
	}, 
	; 323
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_323, i32 0, i32 0); name
	}, 
	; 324
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_324, i32 0, i32 0); name
	}, 
	; 325
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_325, i32 0, i32 0); name
	}, 
	; 326
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_326, i32 0, i32 0); name
	}, 
	; 327
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_327, i32 0, i32 0); name
	}, 
	; 328
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_328, i32 0, i32 0); name
	}, 
	; 329
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_329, i32 0, i32 0); name
	}, 
	; 330
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_330, i32 0, i32 0); name
	}, 
	; 331
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_331, i32 0, i32 0); name
	}, 
	; 332
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_332, i32 0, i32 0); name
	}, 
	; 333
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_333, i32 0, i32 0); name
	}, 
	; 334
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_334, i32 0, i32 0); name
	}, 
	; 335
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_335, i32 0, i32 0); name
	}, 
	; 336
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_336, i32 0, i32 0); name
	}, 
	; 337
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_337, i32 0, i32 0); name
	}, 
	; 338
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_338, i32 0, i32 0); name
	}, 
	; 339
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_339, i32 0, i32 0); name
	}, 
	; 340
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_340, i32 0, i32 0); name
	}, 
	; 341
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_341, i32 0, i32 0); name
	}, 
	; 342
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_342, i32 0, i32 0); name
	}, 
	; 343
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_343, i32 0, i32 0); name
	}, 
	; 344
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_344, i32 0, i32 0); name
	}, 
	; 345
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_345, i32 0, i32 0); name
	}, 
	; 346
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_346, i32 0, i32 0); name
	}, 
	; 347
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_347, i32 0, i32 0); name
	}, 
	; 348
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_348, i32 0, i32 0); name
	}, 
	; 349
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_349, i32 0, i32 0); name
	}, 
	; 350
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_350, i32 0, i32 0); name
	}, 
	; 351
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_351, i32 0, i32 0); name
	}, 
	; 352
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_352, i32 0, i32 0); name
	}, 
	; 353
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_353, i32 0, i32 0); name
	}, 
	; 354
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_354, i32 0, i32 0); name
	}, 
	; 355
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_355, i32 0, i32 0); name
	}, 
	; 356
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_356, i32 0, i32 0); name
	}, 
	; 357
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_357, i32 0, i32 0); name
	}, 
	; 358
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_358, i32 0, i32 0); name
	}, 
	; 359
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_359, i32 0, i32 0); name
	}, 
	; 360
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_360, i32 0, i32 0); name
	}, 
	; 361
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_361, i32 0, i32 0); name
	}, 
	; 362
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_362, i32 0, i32 0); name
	}, 
	; 363
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_363, i32 0, i32 0); name
	}, 
	; 364
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_364, i32 0, i32 0); name
	}, 
	; 365
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_365, i32 0, i32 0); name
	}, 
	; 366
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_366, i32 0, i32 0); name
	}, 
	; 367
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_367, i32 0, i32 0); name
	}, 
	; 368
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_368, i32 0, i32 0); name
	}, 
	; 369
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_369, i32 0, i32 0); name
	}, 
	; 370
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_370, i32 0, i32 0); name
	}, 
	; 371
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_371, i32 0, i32 0); name
	}, 
	; 372
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_372, i32 0, i32 0); name
	}, 
	; 373
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_373, i32 0, i32 0); name
	}, 
	; 374
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_374, i32 0, i32 0); name
	}, 
	; 375
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_375, i32 0, i32 0); name
	}, 
	; 376
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_376, i32 0, i32 0); name
	}, 
	; 377
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_377, i32 0, i32 0); name
	}, 
	; 378
	%struct.XamarinAndroidBundledAssembly {
		i32 -1, ; apk_fd
		i32 0, ; data_offset
		i32 0, ; data_size
		i8* null, ; data
		i32 70, ; name_length
		i8* getelementptr inbounds ([70 x i8], [70 x i8]* @__XamarinAndroidBundledAssembly_name_378, i32 0, i32 0); name
	}
], align 4; end of 'bundled_assemblies' array


; Assembly store individual assembly data
@assembly_store_bundled_assemblies = local_unnamed_addr global [0 x %struct.AssemblyStoreSingleAssemblyRuntimeData] zeroinitializer, align 4

; Assembly store data
@assembly_stores = local_unnamed_addr global [0 x %struct.AssemblyStoreRuntimeData] zeroinitializer, align 4

!llvm.module.flags = !{!0, !1, !2}
!llvm.ident = !{!3}
!0 = !{i32 1, !"wchar_size", i32 4}
!1 = !{i32 7, !"PIC Level", i32 2}
!2 = !{i32 1, !"min_enum_size", i32 4}
!3 = !{!"Xamarin.Android remotes/origin/release/7.0.1xx @ 8f1d9a47205ead80132661f68b0cee9ed0e0220b"}
