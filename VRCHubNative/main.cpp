#include <vector>
#include <string>
#include <iostream>
#include "LibSerials/LibSerials.h"

void CreateDatapack(const std::vector<std::string>& args);
void DatapackExtractor(const std::vector<std::string>& args);

void main(int argc, char* argv[]) {
    std::vector<std::string> args;
    for (int i = 0; i < argc; ++i)
        args.emplace_back(argv[i]);

    // datapack creator
    if (std::find(args.begin(), args.end(), "/create") != args.end()) {
        args.erase(std::find(args.begin(), args.end(), "/create"));
        CreateDatapack(args);
        return;
    }

    // datapack extractor
    if (std::find(args.begin(), args.end(), "/extract") != args.end()) {
        args.erase(std::find(args.begin(), args.end(), "/extract"));
        DatapackExtractor(args);
        return;
    }
    InitializeSerials();

    if (args[1] == "System_HWID") {
        std::cout << System_HWID();
        return;
    }

    if (args[1] == "Baseboard_Manufacturer") {
        std::cout << Baseboard_Manufacturer();
        return;
    }

    if (args[1] == "Baseboard_Product") {
        std::cout << Baseboard_Product();
        return;
    }

    if (args[1] == "Baseboard_Serial") {
        std::cout << Baseboard_Serial();
        return;
    }

    if (args[1] == "CPU_Product") {
        std::cout << CPU_Product();
        return;
    }

    if (args[1] == "CPU_Serial") {
        std::cout << CPU_Serial();
        return;
    }

    if (args[1] == "BIOS_Vendor") {
        std::cout << BIOS_Vendor();
        return;
    }

    if (args[1] == "BIOS_Version") {
        std::cout << BIOS_Version();
        return;
    }

    if (args[1] == "BIOS_Date") {
        std::cout << BIOS_Date();
        return;
    }
}