# Add a test of abci.

# Make sure to run it in the current directory, so it can read the test .abc
# files we have here.
add_executable(abci_test abci_test.cc)
target_link_libraries(abci_test gtest_main abci_test_lib)
add_test(NAME abci_test COMMAND abci_test WORKING_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR})
set_target_properties(abci_test PROPERTIES CMAKE_SKIP_RPATH ON)
