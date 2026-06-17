tasks.register<Delete>("clean") {
    description = "clean build directory"
    delete(rootProject.layout.buildDirectory)
}
