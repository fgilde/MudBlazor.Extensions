; ModuleID = 'obj\Debug\net7.0-android\android\jni_remap.arm64-v8a.ll'
source_filename = "obj\Debug\net7.0-android\android\jni_remap.arm64-v8a.ll"
target datalayout = "e-m:e-i8:8:32-i16:16:32-i64:64-i128:128-n32:64-S128"
target triple = "aarch64-unknown-linux-android"


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

!llvm.module.flags = !{!0, !1, !2, !3, !4, !5}
!llvm.ident = !{!6}
!0 = !{i32 1, !"wchar_size", i32 4}
!1 = !{i32 7, !"PIC Level", i32 2}
!2 = !{i32 1, !"branch-target-enforcement", i32 0}
!3 = !{i32 1, !"sign-return-address", i32 0}
!4 = !{i32 1, !"sign-return-address-all", i32 0}
!5 = !{i32 1, !"sign-return-address-with-bkey", i32 0}
!6 = !{!"Xamarin.Android remotes/origin/release/7.0.1xx @ 8f1d9a47205ead80132661f68b0cee9ed0e0220b"}
