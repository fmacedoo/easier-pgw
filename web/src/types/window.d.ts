declare global {
    interface Window {
        chrome: {
            webview: {
                hostObjects: {
                    gateway: {
                        operations: () => Promise<any>,
                    }
                }
            }
        };
    }
}
export {};