#if false
#include "HTTPClient.h"
#include <iostream>
#include <fstream>

ReliableHTTPClient::ReliableHTTPClient() {
    curl = curl_easy_init();
}

ReliableHTTPClient::~ReliableHTTPClient() {
    curl_easy_cleanup(curl);
    if (headers) curl_slist_free_all(headers);
}

int ReliableHTTPClient::SetHeader(const std::string& name, const std::string& value) {
    header_map[name] = value;
    return 0;
}

int ReliableHTTPClient::RemoveHeader(const std::string& name) {
    header_map.erase(name);
    return 0;
}

std::map<std::string, std::string> ReliableHTTPClient::GetHeaders() {
    return header_map;
}

int ReliableHTTPClient::ClearHeaders() {
    header_map.clear();
    if (headers) {
        curl_slist_free_all(headers);
        headers = nullptr;
    }
    return 0;
}

int ReliableHTTPClient::PerformRequest(RequestType type, const std::string& url) {
    if (!curl) return -1;

    ClearHeaders();
    for (const auto& [name, value] : header_map) {
        std::string header = name + ": " + value;
        headers = curl_slist_append(headers, header.c_str());
    }
    curl_easy_setopt(curl, CURLOPT_HTTPHEADER, headers);
    curl_easy_setopt(curl, CURLOPT_URL, url.c_str());

    switch (type) {
    case RequestType::GET:
        curl_easy_setopt(curl, CURLOPT_HTTPGET, 1L);
        break;
    case RequestType::POST:
        curl_easy_setopt(curl, CURLOPT_POST, 1L);
        break;
    case RequestType::PUT:
        curl_easy_setopt(curl, CURLOPT_CUSTOMREQUEST, "PUT");
        break;
    case RequestType::PATCH:
        curl_easy_setopt(curl, CURLOPT_CUSTOMREQUEST, "PATCH");
        break;
    default:
        return -1;
    }

    return 0;
}
void TrimNewLines(std::string& str) {
    str.erase(std::find_if(str.rbegin(), str.rend(), [](unsigned char ch) {
        return !std::isspace(ch) && ch != '\n' && ch != '\r';
        }).base(), str.end());
}

int ReliableHTTPClient::HTTPRequest(const std::string& url, std::string& response) {
    if (!curl) return -1;

    curl_easy_setopt(curl, CURLOPT_WRITEFUNCTION, WriteToString);
    curl_easy_setopt(curl, CURLOPT_WRITEDATA, &response);

    int result = PerformRequest(RequestType::GET, url);
    if (result != 0) return result;

    CURLcode res = curl_easy_perform(curl);
    if (res == CURLE_OK) {
        TrimNewLines(response);
        return 0;
    }
    return static_cast<int>(res);
}

int ReliableHTTPClient::HTTPRequest(RequestType type, const std::string& url, std::string& response) {
    if (!curl) return -1;

    curl_easy_setopt(curl, CURLOPT_WRITEFUNCTION, WriteToString);
    curl_easy_setopt(curl, CURLOPT_WRITEDATA, &response);

    int result = PerformRequest(type, url);
    if (result != 0) return result;

    CURLcode res = curl_easy_perform(curl);
    if (res == CURLE_OK) {
        TrimNewLines(response);
        return 0;
    }
    return static_cast<int>(res);
}

int ReliableHTTPClient::HTTPRequest(RequestType type, const std::string& url, const std::string& body, std::string& response) {
    if (!curl) return -1;

    curl_easy_setopt(curl, CURLOPT_WRITEFUNCTION, WriteToString);
    curl_easy_setopt(curl, CURLOPT_WRITEDATA, &response);

    // Set the request type and handle request body if necessary
    switch (type) {
    case RequestType::POST:
        curl_easy_setopt(curl, CURLOPT_POST, 1L);
        curl_easy_setopt(curl, CURLOPT_POSTFIELDS, body.c_str());
        curl_easy_setopt(curl, CURLOPT_POSTFIELDSIZE, body.size());
        break;
    case RequestType::PUT:
        curl_easy_setopt(curl, CURLOPT_CUSTOMREQUEST, "PUT");
        curl_easy_setopt(curl, CURLOPT_POSTFIELDS, body.c_str());
        curl_easy_setopt(curl, CURLOPT_POSTFIELDSIZE, body.size());
        break;
    case RequestType::PATCH:
        curl_easy_setopt(curl, CURLOPT_CUSTOMREQUEST, "PATCH");
        curl_easy_setopt(curl, CURLOPT_POSTFIELDS, body.c_str());
        curl_easy_setopt(curl, CURLOPT_POSTFIELDSIZE, body.size());
        break;
    default:
        return -1; // Unsupported request type for body
    }

    // Set the URL for the request
    curl_easy_setopt(curl, CURLOPT_URL, url.c_str());

    // Perform the request
    CURLcode res = curl_easy_perform(curl);
    if (res == CURLE_OK) {
        TrimNewLines(response);
        return 0;
    }
    return static_cast<int>(res);
}

int ReliableHTTPClient::GetBytes(const std::string& url, std::vector<uint8_t>& bytes) {
    if (!curl) return -1;

    curl_easy_setopt(curl, CURLOPT_WRITEFUNCTION, WriteToBytes);
    curl_easy_setopt(curl, CURLOPT_WRITEDATA, &bytes);

    int result = PerformRequest(RequestType::GET, url);
    if (result != 0) return result;

    CURLcode res = curl_easy_perform(curl);
    return res == CURLE_OK ? 0 : static_cast<int>(res);
}
int ReliableHTTPClient::GetBytes(RequestType type, const std::string& url, std::vector<uint8_t>& bytes) {
    if (!curl) return -1;

    curl_easy_setopt(curl, CURLOPT_WRITEFUNCTION, WriteToBytes);
    curl_easy_setopt(curl, CURLOPT_WRITEDATA, &bytes);

    int result = PerformRequest(type, url);
    if (result != 0) return result;

    CURLcode res = curl_easy_perform(curl);
    return res == CURLE_OK ? 0 : static_cast<int>(res);
}

int ReliableHTTPClient::DownloadFile(RequestType type, const std::string& url, const std::string& filepath) {
    if (!curl) return -1;

    FILE* file = nullptr;
    errno_t err = fopen_s(&file, filepath.c_str(), "wb");
    if (err != 0 || !file) {
        return -2;
    }

    curl_easy_setopt(curl, CURLOPT_WRITEFUNCTION, WriteToFile);
    curl_easy_setopt(curl, CURLOPT_WRITEDATA, file);

    int result = PerformRequest(type, url);
    if (result != 0) {
        fclose(file);
        return result;
    }

    CURLcode res = curl_easy_perform(curl);
    fclose(file);
    return res == CURLE_OK ? 0 : static_cast<int>(res);
}
int ReliableHTTPClient::DownloadFile(const std::string& url, const std::string& filepath) {
    if (!curl) return -1;

    FILE* file = nullptr;
    errno_t err = fopen_s(&file, filepath.c_str(), "wb");
    if (err != 0 || !file) {
        return -2;
    }

    curl_easy_setopt(curl, CURLOPT_WRITEFUNCTION, WriteToFile);
    curl_easy_setopt(curl, CURLOPT_WRITEDATA, file);

    int result = PerformRequest(RequestType::GET, url);
    if (result != 0) {
        fclose(file);
        return result;
    }

    CURLcode res = curl_easy_perform(curl);
    fclose(file);
    return res == CURLE_OK ? 0 : static_cast<int>(res);
}

size_t ReliableHTTPClient::WriteToString(void* ptr, size_t size, size_t nmemb, std::string* data) {
    if (!data || !ptr) {
        return 0;
    }
    data->append(static_cast<char*>(ptr), size * nmemb);
    return size * nmemb;
}

size_t ReliableHTTPClient::WriteToFile(void* ptr, size_t size, size_t nmemb, FILE* stream) {
    if (!stream || !ptr) {
        return 0;
    }
    return fwrite(ptr, size, nmemb, stream);
}

size_t ReliableHTTPClient::WriteToBytes(void* ptr, size_t size, size_t nmemb, std::vector<uint8_t>* data) {
    if (!data || !ptr) {
        return 0;
    }
    data->insert(data->end(), static_cast<uint8_t*>(ptr), static_cast<uint8_t*>(ptr) + size * nmemb);
    return size * nmemb;
}
#endif