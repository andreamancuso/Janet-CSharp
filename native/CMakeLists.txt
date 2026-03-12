cmake_minimum_required(VERSION 3.15)
project(janet_shim C)

set(JANET_SRC_DIR "${CMAKE_SOURCE_DIR}/../extern/janet/src")

# Collect all Janet core source files
file(GLOB JANET_CORE_SOURCES "${JANET_SRC_DIR}/core/*.c")

# Our shim source
set(SHIM_SOURCES janet_shim.c)

# Build shared library
add_library(janet_shim SHARED ${JANET_CORE_SOURCES} ${SHIM_SOURCES})

target_include_directories(janet_shim PRIVATE
    "${JANET_SRC_DIR}/include"
    "${JANET_SRC_DIR}/conf"
)

# JANET_BOOTSTRAP: use C-level janet_core_env() without needing the
# pre-compiled boot image (amalgamation). Gives us all C-level built-in
# functions which is sufficient for Phase 1.
target_compile_definitions(janet_shim PRIVATE
    JANET_BOOTSTRAP
)

# Platform-specific settings
if(WIN32)
    # MSVC: disable secure CRT warnings
    target_compile_definitions(janet_shim PRIVATE _CRT_SECURE_NO_WARNINGS)
    target_compile_options(janet_shim PRIVATE /W3)
    target_link_libraries(janet_shim PRIVATE ws2_32)
elseif(APPLE)
    target_compile_options(janet_shim PRIVATE -fPIC -Wall -Wextra)
    target_link_libraries(janet_shim PRIVATE m pthread)
else()
    target_compile_options(janet_shim PRIVATE -fPIC -Wall -Wextra)
    target_link_libraries(janet_shim PRIVATE m pthread dl)
endif()

# Install the built library next to the build output for easy pickup by .NET
install(TARGETS janet_shim
    RUNTIME DESTINATION ${CMAKE_INSTALL_PREFIX}
    LIBRARY DESTINATION ${CMAKE_INSTALL_PREFIX}
)
