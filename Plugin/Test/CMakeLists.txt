if(USDI_ENABLE_TESTS)
    set(CMAKE_INSTALL_RPATH "${CMAKE_INSTALL_PREFIX}")
    file(GLOB sources *.cpp *.h)

    add_executable(Test ${sources})
    add_dependencies(Test abci)
    target_link_libraries(Test abci)
endif()
