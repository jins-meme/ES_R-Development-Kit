//
//  SettingViewController.swift
//  MEME_Academic
//
//  Created by Celleus on 2022/09/02.
//  Copyright © 2022 jins-jp. All rights reserved.
//

import Cocoa

@MainActor
protocol SettingViewControllerDelegate: AnyObject {
    func didApply(_ settingViewController: SettingViewController)
}

class SettingViewController: NSViewController {

    weak var delegate: SettingViewControllerDelegate?

    @IBOutlet weak var button_Browse: NSButton!
    @IBOutlet weak var button_OpenFolder: NSButton!
    @IBOutlet weak var button_Apply: NSButton!
    @IBOutlet weak var button_Cancel: NSButton!

    @IBOutlet weak var browseTextField: NSTextField!

    @IBOutlet weak var xAxisTextField: NSTextFieldCell!
    @IBOutlet weak var yAxisTextField: NSTextFieldCell!
    @IBOutlet weak var zAxisTextField: NSTextFieldCell!

    @IBOutlet weak var showSaveFileDialogButton: NSButton!
    @IBOutlet weak var extermalOutputSocketButton: NSButton!

    @IBOutlet weak var localProtTextField: NSTextFieldCell!
    @IBOutlet weak var localAddressTextField: NSTextField!

    override func viewDidLoad() {
        super.viewDidLoad()
        setSettingParameter()
        styleButtonsForAppearance()
    }

    override func viewWillAppear() {
        super.viewWillAppear()
        view.window?.title = "Setting"
    }

    func setSettingParameter() {
        NSLog("setSettingParameter")
        browseTextField.stringValue = UserSetting.getSaveFilePath()
        xAxisTextField.stringValue = String(format: "%g", UserSetting.getXAxis())
        yAxisTextField.stringValue = String(format: "%g", UserSetting.getYAxis())
        zAxisTextField.stringValue = String(format: "%g", UserSetting.getZAxis())
        showSaveFileDialogButton.state = UserSetting.getShowSaveFileDialog() ? .on : .off
        extermalOutputSocketButton.state = UserSetting.getExtermalOutputSocket() ? .on : .off
        localProtTextField.stringValue = UserSetting.getLocalPort()
        localAddressTextField.stringValue = Common.getIPAddress()
    }

    func saveSettingParameter() {
        NSLog("saveSettingParameter")
        UserSetting.setSaveFilePath(browseTextField.stringValue)
        UserSetting.setXAxis(Double(xAxisTextField.stringValue) ?? 0)
        UserSetting.setYAxis(Double(yAxisTextField.stringValue) ?? 0)
        UserSetting.setZAxis(Double(zAxisTextField.stringValue) ?? 0)
        UserSetting.setShowSaveFileDialog(showSaveFileDialogButton.state == .on)
        UserSetting.setExtermalOutputSocket(extermalOutputSocketButton.state == .on)
        UserSetting.setLocalPort(localProtTextField.stringValue)
    }

    @IBAction func button_Browse_Tapped(_ sender: Any) {
        NSLog("button_Browse_Tapped")
        showOpenPanel()
    }

    private func showOpenPanel() {
        let panel = NSOpenPanel()
        panel.canChooseDirectories = true
        panel.canChooseFiles = false
        guard let window = view.window else { return }
        panel.beginSheetModal(for: window) { [weak self] result in
            if result == .OK, let url = panel.url {
                self?.browseTextField.stringValue = url.path
            }
        }
    }

    @IBAction func button_OpenFolder_Tapped(_ sender: Any) {
        NSLog("button_OpenFolder_Tapped")
        showOpenFolder()
    }

    private func showOpenFolder() {
        let urlString = browseTextField.stringValue
        let url = URL(fileURLWithPath: urlString)
        if NSWorkspace.shared.open(url) {
            NSLog("ファイルが開ける")
        } else {
            NSLog("ファイルが開けない")
        }
    }

    @IBAction func button_Extemal_Output_Socket_Tapped(_ sender: Any) {
        NSLog("button_Extemal_Output_Socket_Tapped")
    }

    @IBAction func textField_Local_Port_Tapped(_ sender: Any) {
        NSLog("textField_Local_Port_Tapped")
    }

    @IBAction func button_Apply_Tapped(_ sender: Any) {
        NSLog("button_Apply_Tapped")
        saveSettingParameter()
        delegate?.didApply(self)
        view.window?.close()
    }

    @IBAction func button_Cancel_Tapped(_ sender: Any) {
        NSLog("button_Cancel_Tapped")
        view.window?.close()
    }

    private func styleButtonsForAppearance() {
        let appearanceName = view.effectiveAppearance.bestMatch(from: [.aqua, .darkAqua])
        let isDark = appearanceName == .darkAqua
        let bgWhite: CGFloat = isDark ? 0.60 : 0.85
        let titleColor: NSColor = isDark ? .labelColor : NSColor(white: 0.15, alpha: 1.0)
        applyStyle(toButtonsIn: view, bgWhite: bgWhite, titleColor: titleColor)
    }

    private func applyStyle(toButtonsIn parent: NSView, bgWhite: CGFloat, titleColor: NSColor) {
        for sub in parent.subviews {
            if let button = sub as? NSButton {
                if button == showSaveFileDialogButton || button == extermalOutputSocketButton {
                    // skip
                } else {
                    button.wantsLayer = true
                    button.layer?.cornerRadius = 14.0
                    button.layer?.masksToBounds = true
                    button.layer?.backgroundColor = NSColor(white: bgWhite, alpha: 1.0).cgColor
                    button.isBordered = false
                    if let cell = button.cell as? NSButtonCell {
                        cell.isBordered = false
                    }

                    var useTitleColor = titleColor
                    if button == button_Browse || button == button_OpenFolder || button == button_Apply || button == button_Cancel {
                        useTitleColor = NSColor(white: 0.15, alpha: 1.0)
                    }

                    let attr = NSMutableAttributedString(attributedString: button.attributedTitle)
                    let range = NSRange(location: 0, length: attr.length)
                    attr.addAttribute(.foregroundColor, value: useTitleColor, range: range)
                    button.attributedTitle = attr
                }
            }
            applyStyle(toButtonsIn: sub, bgWhite: bgWhite, titleColor: titleColor)
        }
    }
}
