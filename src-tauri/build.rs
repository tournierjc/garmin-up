fn main() {
    println!("cargo:rerun-if-changed=proto/omt_map_update.proto");

    // Compile the minimal subset of Garmin Express ProtoBufService DTOs we need.
    //
    // These protobuf schemas are inferred from the Garmin Express DTO assembly shipped
    // alongside the Windows version of Express (field ordering + names).
    prost_build::Config::new()
        .compile_protos(&["proto/omt_map_update.proto"], &["proto"])
        .expect("failed to compile protobuf schemas");

    tauri_build::build()
}
