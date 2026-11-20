// Ghidra headless query script for legacy-explore (Tier 3 ELF analysis).
//@category LegacyExplore

import java.util.ArrayList;
import java.util.List;
import java.util.regex.Pattern;

import ghidra.app.decompiler.DecompInterface;
import ghidra.app.decompiler.DecompileResults;
import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.Data;
import ghidra.program.model.listing.DataIterator;
import ghidra.program.model.listing.Function;
import ghidra.program.model.listing.FunctionIterator;
import ghidra.program.model.symbol.Reference;
import ghidra.program.model.symbol.ReferenceIterator;

public class JxExploreGhidraQuery extends GhidraScript {

    private static final String TAG = "[JX-GHIDRA] ";

    private void emit(String msg) {
        println(TAG + msg);
    }

    @Override
    public void run() throws Exception {
        String[] args = getScriptArgs();
        if (args == null || args.length == 0) {
            String envCmd = System.getenv("JX_GHIDRA_CMD");
            if (envCmd != null && !envCmd.isEmpty()) {
                String a1 = System.getenv("JX_GHIDRA_ARG1");
                String a2 = System.getenv("JX_GHIDRA_ARG2");
                if (a2 != null && !a2.isEmpty()) {
                    args = new String[] { envCmd, a1 != null ? a1 : "", a2 };
                } else if (a1 != null && !a1.isEmpty()) {
                    args = new String[] { envCmd, a1 };
                } else {
                    args = new String[] { envCmd };
                }
            }
        }
        if (args == null || args.length == 0) {
            emit("ERROR: Missing command. Usage: search-funcs <regex> [max] | decompile <name_or_0xaddr> | search-strings <substr> [max] | xrefs <name_or_0xaddr> [max]");
            return;
        }

        String cmd = args[0];
        switch (cmd) {
            case "search-funcs":
                cmdSearchFuncs(args);
                break;
            case "decompile":
                cmdDecompile(args);
                break;
            case "search-strings":
                cmdSearchStrings(args);
                break;
            case "xrefs":
                cmdXrefs(args);
                break;
            default:
                emit("ERROR: Unknown command: " + cmd);
                break;
        }
    }

    private void cmdSearchFuncs(String[] args) {
        if (args.length < 2) {
            emit("ERROR: search-funcs requires <regex>");
            return;
        }
        Pattern pat = Pattern.compile(args[1], Pattern.CASE_INSENSITIVE);
        int max = args.length >= 3 ? Integer.parseInt(args[2]) : 50;

        emit("SEARCH_FUNCS pattern=" + args[1] + " program=" + currentProgram.getName());
        int count = 0;
        FunctionIterator it = currentProgram.getFunctionManager().getFunctions(true);
        while (it.hasNext()) {
            Function fn = it.next();
            String fullName = fn.getName(true);
            String shortName = fn.getName(false);
            if (pat.matcher(fullName).find() || pat.matcher(shortName).find()) {
                long size = fn.getBody().getNumAddresses();
                emit("FUNC 0x" + fn.getEntryPoint().toString() + " | size=" + size + " | " + fullName);
                count++;
                if (count >= max) {
                    emit("TRUNCATED at max=" + max);
                    break;
                }
            }
        }
        emit("TOTAL_MATCHES=" + count);
    }

    private List<Function> resolveFunctions(String target) {
        List<Function> exact = new ArrayList<>();
        List<Function> partial = new ArrayList<>();

        if (target.startsWith("0x") || target.startsWith("0X")) {
            Address addr = toAddr(target);
            if (addr != null) {
                Function fn = getFunctionAt(addr);
                if (fn == null) {
                    fn = getFunctionContaining(addr);
                }
                if (fn != null) {
                    exact.add(fn);
                }
            }
            return exact;
        }

        String lower = target.toLowerCase();
        FunctionIterator it = currentProgram.getFunctionManager().getFunctions(true);
        while (it.hasNext()) {
            Function fn = it.next();
            String fullName = fn.getName(true);
            String shortName = fn.getName(false);
            if (fullName.equals(target) || shortName.equals(target)) {
                exact.add(fn);
            } else if (fullName.toLowerCase().contains(lower)) {
                partial.add(fn);
            }
        }
        return !exact.isEmpty() ? exact : partial;
    }

    private void cmdDecompile(String[] args) throws Exception {
        if (args.length < 2) {
            emit("ERROR: decompile requires <name_or_0xaddr>");
            return;
        }
        String target = args[1];
        List<Function> matches = resolveFunctions(target);
        if (matches.isEmpty()) {
            emit("ERROR: No function found matching: " + target);
            return;
        }
        if (matches.size() > 1) {
            emit("MULTIPLE_MATCHES (" + matches.size() + ") for '" + target + "' — pass exact 0xAddr:");
            for (int i = 0; i < Math.min(20, matches.size()); i++) {
                Function f = matches.get(i);
                emit("  CANDIDATE 0x" + f.getEntryPoint().toString() + " | " + f.getName(true));
            }
            return;
        }

        Function fn = matches.get(0);
        DecompInterface ifc = new DecompInterface();
        ifc.openProgram(currentProgram);
        try {
            DecompileResults res = ifc.decompileFunction(fn, 60, monitor);
            if (res == null || !res.decompileCompleted() || res.getDecompiledFunction() == null) {
                String err = res != null ? res.getErrorMessage() : "unknown decompile error";
                emit("ERROR: Decompile failed for 0x" + fn.getEntryPoint().toString() + ": " + err);
                return;
            }
            emit("DECOMPILE_BEGIN 0x" + fn.getEntryPoint().toString() + " | " + fn.getName(true));
            String code = res.getDecompiledFunction().getC();
            for (String line : code.split("\\r?\\n")) {
                emit("C| " + line);
            }
            emit("DECOMPILE_END 0x" + fn.getEntryPoint().toString());
        } finally {
            ifc.dispose();
        }
    }

    private void cmdSearchStrings(String[] args) {
        if (args.length < 2) {
            emit("ERROR: search-strings requires <substring>");
            return;
        }
        String query = args[1].toLowerCase();
        int max = args.length >= 3 ? Integer.parseInt(args[2]) : 50;

        emit("SEARCH_STRINGS query=" + args[1] + " program=" + currentProgram.getName());
        int count = 0;
        DataIterator it = currentProgram.getListing().getDefinedData(true);
        while (it.hasNext()) {
            Data d = it.next();
            if (!d.hasStringValue()) {
                continue;
            }
            Object valObj = d.getValue();
            if (valObj == null) {
                continue;
            }
            String val = valObj.toString();
            if (val.toLowerCase().contains(query)) {
                Reference[] refs = getReferencesTo(d.getAddress());
                String sanitized = val.replace("\r", "\\r").replace("\n", "\\n");
                if (sanitized.length() > 160) {
                    sanitized = sanitized.substring(0, 160) + "...";
                }
                emit("STR 0x" + d.getAddress().toString() + " | refs=" + refs.length + " | " + sanitized);
                count++;
                if (count >= max) {
                    emit("TRUNCATED at max=" + max);
                    break;
                }
            }
        }
        emit("TOTAL_STRINGS=" + count);
    }

    private void cmdXrefs(String[] args) {
        if (args.length < 2) {
            emit("ERROR: xrefs requires <name_or_0xaddr>");
            return;
        }
        String target = args[1];
        int max = args.length >= 3 ? Integer.parseInt(args[2]) : 50;

        Address addr = null;
        if (target.startsWith("0x") || target.startsWith("0X")) {
            addr = toAddr(target);
        } else {
            List<Function> matches = resolveFunctions(target);
            if (matches.size() == 1) {
                addr = matches.get(0).getEntryPoint();
            } else if (matches.size() > 1) {
                emit("MULTIPLE_MATCHES (" + matches.size() + ") for '" + target + "' — pass exact 0xAddr");
                return;
            }
        }

        if (addr == null) {
            emit("ERROR: Cannot resolve address for: " + target);
            return;
        }

        emit("XREFS_TO 0x" + addr.toString());
        int count = 0;
        ReferenceIterator it = currentProgram.getReferenceManager().getReferencesTo(addr);
        while (it.hasNext()) {
            Reference ref = it.next();
            Address from = ref.getFromAddress();
            Function caller = getFunctionContaining(from);
            String callerName = caller != null ? caller.getName(true) : "<no-func>";
            emit("XREF from=0x" + from.toString() + " | type=" + ref.getReferenceType().getName() + " | in=" + callerName);
            count++;
            if (count >= max) {
                emit("TRUNCATED at max=" + max);
                break;
            }
        }
        emit("TOTAL_XREFS=" + count);
    }
}
