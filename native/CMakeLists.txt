cmake_minimum_required(VERSION 3.16)
project(recall_embed C CXX)

set(CMAKE_C_STANDARD 11)
set(CMAKE_CXX_STANDARD 17)

if(CMAKE_SYSTEM_NAME STREQUAL "Android")
    if(NOT DEFINED ENV{ANDROID_NDK_HOME} OR "$ENV{ANDROID_NDK_HOME}" STREQUAL "")
        message(FATAL_ERROR "ANDROID_NDK_HOME is required for the Android llama.cpp build.")
    endif()
endif()

include(FetchContent)

set(LLAMA_BUILD_COMMON OFF CACHE BOOL "" FORCE)
set(LLAMA_BUILD_TOOLS OFF CACHE BOOL "" FORCE)
set(LLAMA_BUILD_TESTS OFF CACHE BOOL "" FORCE)
set(LLAMA_BUILD_EXAMPLES OFF CACHE BOOL "" FORCE)
set(LLAMA_BUILD_SERVER OFF CACHE BOOL "" FORCE)
set(GGML_NATIVE OFF CACHE BOOL "" FORCE)
set(GGML_CUDA OFF CACHE BOOL "" FORCE)
set(BUILD_SHARED_LIBS OFF CACHE BOOL "" FORCE)

if(APPLE)
    set(GGML_METAL ON CACHE BOOL "" FORCE)
    set(GGML_VULKAN OFF CACHE BOOL "" FORCE)
elseif(WIN32)
    find_package(Vulkan QUIET)
    if(NOT Vulkan_FOUND)
        message(FATAL_ERROR "Vulkan SDK is required for the Windows llama.cpp build.")
    endif()
    set(GGML_VULKAN ON CACHE BOOL "" FORCE)
elseif(ANDROID)
    set(GGML_VULKAN OFF CACHE BOOL "" FORCE)
else()
    set(GGML_VULKAN ON CACHE BOOL "" FORCE)
endif()

set(_llama_vendor "${CMAKE_CURRENT_SOURCE_DIR}/vendor/llama.cpp")
if(EXISTS "${_llama_vendor}/CMakeLists.txt")
  set(FETCHCONTENT_SOURCE_DIR_LLAMA "${_llama_vendor}")
endif()

FetchContent_Declare(
  llama
  GIT_REPOSITORY https://github.com/ggml-org/llama.cpp.git
  GIT_TAG b11455
  GIT_SHALLOW TRUE
  GIT_PROGRESS TRUE
)
FetchContent_MakeAvailable(llama)

set(MTMD_VIDEO OFF CACHE BOOL "" FORCE)
add_subdirectory(${llama_SOURCE_DIR}/tools/mtmd ${CMAKE_CURRENT_BINARY_DIR}/mtmd)

add_library(recall_embed SHARED recall_embed.c)
set_source_files_properties(recall_embed.c PROPERTIES LANGUAGE CXX)
target_link_libraries(recall_embed PRIVATE llama mtmd)
target_include_directories(recall_embed PRIVATE ${llama_SOURCE_DIR}/include ${llama_SOURCE_DIR}/tools/mtmd)
