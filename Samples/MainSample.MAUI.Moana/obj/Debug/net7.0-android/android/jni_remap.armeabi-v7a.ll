; ModuleID = 'obj\Debug\net7.0-android\android\jni_remap.armeabi-v7a.ll'
source_filename = "obj\Debug\net7.0-android\android\jni_remap.armeabi-v7a.ll"
target datalayout = "e-m:e-p:32:32-Fi8-i64:64-v128:64:128-a:0:32-n32-S64"
target triple = "armv7-unknown-linux-android"


%struct.JniRemappingString = type {
	i32,; uint32_t length
	i8*; char* str
}

%struct.JniRemappingReplacementMethod = type {
	i8*,; char* target_type
	i8*,; char* target_name
	i8; bool is_static
}

%struct.JniRemappingIndexMethodEntry = type {
	%struct.JniRemappingString,; JniRemappingString name
	%struct.JniRemappingString,; JniRemappingString signature
	%struct.JniRemappingReplacementMethod; JniRemappingReplacementMethod replacement
}

%struct.JniRemappingIndexTypeEntry = type {
	%struct.JniRemappingString,; JniRemappingString name
	i32,; uint32_t method_count
	%struct.JniRemappingIndexMethodEntry*; JniRemappingIndexMethodEntry* methods
}

%struct.JniRemappingTypeReplacementEntry = type {
	%struct.JniRemappingString,; JniRemappingString name
	i8*; char* replacement
}

; JNI remapping data

; jni_remapping_type_replacements
@jni_remapping_type_replacements = local_unnamed_addr constant [0 x %struct.JniRemappingTypeReplacementEntry] zeroinitializer, align 4

; jni_remapping_method_replacement_index
@jni_remapping_method_replacement_index = local_unnamed_addr constant [0 x %struct.JniRemappingIndexTypeEntry] zeroinitializer, align 4

!llvm.module.flags = !{!0, !1, !2}
!llvm.ident = !{!3}
!0 = !{i32 1, !"wchar_size", i32 4}
!1 = !{i32 7, !"PIC Level", i32 2}
!2 = !{i32 1, !"min_enum_size", i32 4}
!3 = !{!"Xamarin.Android remotes/origin/release/7.0.1xx @ 8f1d9a47205ead80132661f68b0cee9ed0e0220b"}
