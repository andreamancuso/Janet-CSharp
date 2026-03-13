cmake_minimum_required(VERSION 3.15)
project(janet_shim C)

set(JANET_ROOT "${CMAKE_SOURCE_DIR}/../extern/janet")
set(JANET_SRC_DIR "${JANET_ROOT}/src")

# Collect Janet core sources (used by both boot exe and final library)
file(GLOB JANET_CORE_SOURCES "${JANET_SRC_DIR}/core/*.c")

# Shared compile definitions: disable unused subsystems that cause
# platform-specific issues (e.g. kqueue init crash on macOS ARM64 CI)
# and aren't needed since JanetSharp has its own P/Invoke layer.
set(JANET_NO_FLAGS
    JANET_NO_EV
    JANET_NO_FFI
    JANET_NO_NET
    JANET_NO_DYNAMIC_MODULES
)

# Helper: apply platform-specific settings to a target
function(janet_platform_settings target)
    if(WIN32)
        target_compile_definitions(${target} PRIVATE _CRT_SECURE_NO_WARNINGS)
        target_compile_options(${target} PRIVATE /W3)
        target_link_libraries(${target} PRIVATE ws2_32)
    elseif(APPLE)
        target_compile_options(${target} PRIVATE -fPIC -Wall -Wextra)
        target_link_libraries(${target} PRIVATE m pthread)
    else()
        target_compile_options(${target} PRIVATE -fPIC -Wall -Wextra)
        target_link_libraries(${target} PRIVATE m pthread dl)
    endif()
endfunction()

# ============================================================
# Stage 1: Build the bootstrap interpreter (janet_boot)
#
# Compiled WITH JANET_BOOTSTRAP — has only C-level primitives.
# Runs boot.janet to generate the full stdlib image.
# ============================================================

file(GLOB JANET_BOOT_SOURCES "${JANET_SRC_DIR}/boot/*.c")

add_executable(janet_boot ${JANET_CORE_SOURCES} ${JANET_BOOT_SOURCES})

target_include_directories(janet_boot PRIVATE
    "${JANET_SRC_DIR}/include"
    "${JANET_SRC_DIR}/conf"
)

target_compile_definitions(janet_boot PRIVATE
    JANET_BOOTSTRAP
    ${JANET_NO_FLAGS}
)

janet_platform_settings(janet_boot)

# ============================================================
# Stage 2: Generate the core image
#
# janet_boot reads boot.janet, compiles the full Janet stdlib,
# marshals the environment into a C byte array, and writes it
# to stdout. The "image-only" flag skips amalgamation of source
# files — we just need the serialized image data.
# ============================================================

set(JANET_IMAGE_FILE "${CMAKE_BINARY_DIR}/janet_core_image.c")

add_custom_command(
    OUTPUT "${JANET_IMAGE_FILE}"
    COMMAND ${CMAKE_COMMAND} -E env
        "$<TARGET_FILE:janet_boot>" "${JANET_ROOT}" image-only > "${JANET_IMAGE_FILE}"
    DEPENDS janet_boot "${JANET_ROOT}/src/boot/boot.janet"
    COMMENT "Generating Janet core image from boot.janet"
)

# ============================================================
# Stage 3: Build the final shared library (janet_shim)
#
# Compiled WITHOUT JANET_BOOTSTRAP — janet_core_env() unmarshals
# the pre-built image, returning the complete Janet environment
# with all ~500 stdlib functions.
# ============================================================

set(SHIM_SOURCES janet_shim.c)

add_library(janet_shim SHARED
    ${JANET_CORE_SOURCES}
    "${JANET_IMAGE_FILE}"
    ${SHIM_SOURCES}
)

target_include_directories(janet_shim PRIVATE
    "${JANET_SRC_DIR}/include"
    "${JANET_SRC_DIR}/conf"
)

# NO JANET_BOOTSTRAP here — use the full stdlib from the generated image
target_compile_definitions(janet_shim PRIVATE
    ${JANET_NO_FLAGS}
)

janet_platform_settings(janet_shim)

# Install the built library next to the build output for easy pickup by .NET
install(TARGETS janet_shim
    RUNTIME DESTINATION ${CMAKE_INSTALL_PREFIX}
    LIBRARY DESTINATION ${CMAKE_INSTALL_PREFIX}
)
