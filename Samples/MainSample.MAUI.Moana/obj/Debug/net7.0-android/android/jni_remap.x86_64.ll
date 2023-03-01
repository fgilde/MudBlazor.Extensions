; ModuleID = 'obj\Debug\net7.0-android\android\jni_remap.x86_64.ll'
source_filename = "obj\Debug\net7.0-android\android\jni_remap.x86_64.ll"
target datalayout = "e-m:e-p270:32:32-p271:32:32-p272:64:64-i64:64-f80:128-n8:16:32:64-S128"
target triple = "x86_64-unknown-linux-android"


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
@jni_remapping_type_replacements = local_unnamed_addr constant [0 x %struct.JniRemappingTypeReplacementEntry] zeroinitializer, align 8

; jni_remapping_method_replacement_index
@jni_remapping_method_replacement_index = local_unnamed_addr constant [0 x %struct.JniRemappingIndexTypeEntry] zeroinitializer, align 8

!llvm.module.flags = !{!0, !1}
!llvm.ident = !{!2}
!0 = !{i32 1, !"wchar_size", i32 4}
!1 = !{i32 7, !"PIC Level", i32 2}
!2 = !{!"Xamarin.Android remotes/origin/release/7.0.1xx @ 8f1d9a47205ead80132661f68b0cee9ed0e0220b"}
