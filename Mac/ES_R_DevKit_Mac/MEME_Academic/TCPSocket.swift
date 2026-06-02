//
//  TCPSocket.swift
//  MEME_Academic
//
//  Created by Celleus on 2022/09/13.
//  Copyright © 2022 jins-jp. All rights reserved.
//

import Foundation
import Network

@MainActor
protocol TcpSocketDelegate: AnyObject {
    func didAccept()
    func socketDidDisconnect(error: Error?)
}

@MainActor
class TCPSocket: NSObject {
    var headerString: String = ""
    weak var delegate: TcpSocketDelegate?

    private var listener: NWListener?
    private var connection: NWConnection?
    private let queue: DispatchQueue = .main
    private var connected: Bool = false
    private var manuallyStopped: Bool = false

    func start() -> String {
        stop()
        manuallyStopped = false

        let portString = UserSetting.getLocalPort()
        NSLog("portport:%@", portString)
        guard let portNumber = UInt16(portString), let port = NWEndpoint.Port(rawValue: portNumber) else {
            NSLog("Invalid port")
            return "Listen Error"
        }

        do {
            let listener = try NWListener(using: .tcp, on: port)
            self.listener = listener

            listener.stateUpdateHandler = { [weak self] state in
                guard let self = self else { return }
                Task { @MainActor in
                    if case .failed(let error) = state {
                        NSLog("Listener failed: %@", "\(error)")
                        self.notifyDisconnect(error: error)
                    }
                }
            }

            listener.newConnectionHandler = { [weak self] newConnection in
                Task { @MainActor in
                    guard let self = self else {
                        newConnection.cancel()
                        return
                    }
                    if self.connection != nil {
                        newConnection.cancel()
                        return
                    }
                    self.connection = newConnection
                    self.startConnection(newConnection)
                    self.listener?.cancel()
                    self.listener = nil
                }
            }

            listener.start(queue: queue)
            return "Listen"
        } catch {
            NSLog("Error in NWListener: %@", error.localizedDescription)
            return "Listen Error"
        }
    }

    func writeData(_ string: String) {
        guard let data = string.data(using: .utf8) else { return }
        sendData(data)
    }

    func writeHeader() {
        guard let data = headerString.data(using: .utf8) else { return }
        sendData(data)
    }

    func stop() {
        manuallyStopped = true
        connected = false
        listener?.cancel()
        listener = nil
        connection?.cancel()
        connection = nil
    }

    func isConnected() -> Bool {
        return connected
    }

    private func startConnection(_ connection: NWConnection) {
        connection.stateUpdateHandler = { [weak self] state in
            guard let self = self else { return }
            Task { @MainActor in
                guard self.connection === connection else { return }
                switch state {
                case .ready:
                    NSLog("Accepted new Network.framework connection")
                    self.connected = true
                    self.delegate?.didAccept()
                    self.writeHeader()
                    self.receiveData(from: connection)
                case .failed(let error):
                    NSLog("Connection failed: %@", "\(error)")
                    self.notifyDisconnect(error: error)
                case .cancelled:
                    self.notifyDisconnect(error: nil)
                default:
                    break
                }
            }
        }
        connection.start(queue: queue)
    }

    private func sendData(_ data: Data) {
        guard let connection = connection, !data.isEmpty else { return }
        connection.send(content: data, completion: .contentProcessed { [weak self] error in
            guard let error = error else { return }
            Task { @MainActor in
                guard let self = self else { return }
                NSLog("Connection send error: %@", "\(error)")
                self.notifyDisconnect(error: error)
            }
        })
    }

    private func receiveData(from connection: NWConnection) {
        connection.receive(minimumIncompleteLength: 1, maximumLength: 65536) { [weak self] _, _, isComplete, error in
            Task { @MainActor in
                guard let self = self, self.connection === connection else { return }
                if let error = error {
                    NSLog("Connection receive error: %@", "\(error)")
                    self.notifyDisconnect(error: error)
                    return
                }
                if isComplete {
                    self.notifyDisconnect(error: nil)
                    return
                }
                self.receiveData(from: connection)
            }
        }
    }

    private func notifyDisconnect(error: Error?) {
        if manuallyStopped { return }
        connected = false
        listener?.cancel()
        listener = nil
        connection?.cancel()
        connection = nil
        delegate?.socketDidDisconnect(error: error)
    }
}
