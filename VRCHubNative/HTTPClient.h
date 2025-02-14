#if false
#pragma once

#include <string>
#include <vector>
#include <map>
#include <curl/curl.h>

enum class RequestType {
    GET,
    POST,
    PUT,
    PATCH
};

class ReliableHTTPClient {
public:
    ReliableHTTPClient();
    ~ReliableHTTPClient();

    // Header manipulation
    int SetHeader(const std::string& name, const std::string& value);
    int RemoveHeader(const std::string& name);
    std::map<std::string, std::string> GetHeaders();
    int ClearHeaders();

    // HTTP Requests
    int HTTPRequest(const std::string& url, std::string& response);
    int HTTPRequest(RequestType type, const std::string& url, std::string& response);
    int HTTPRequest(RequestType type, const std::string& url, const std::string& body, std::string& response);
    int GetBytes(const std::string& url, std::vector<uint8_t>& bytes);
    int GetBytes(RequestType type, const std::string& url, std::vector<uint8_t>& bytes);
    int DownloadFile(const std::string& url, const std::string& filepath);
    int DownloadFile(RequestType type, const std::string& url, const std::string& filepath);

private:
    static size_t WriteToString(void* ptr, size_t size, size_t nmemb, std::string* data);
    static size_t WriteToFile(void* ptr, size_t size, size_t nmemb, FILE* stream);
    static size_t WriteToBytes(void* ptr, size_t size, size_t nmemb, std::vector<uint8_t>* data);

    CURL* curl;
    struct curl_slist* headers = nullptr;
    std::map<std::string, std::string> header_map;

    int PerformRequest(RequestType type, const std::string& url);
};
#endif