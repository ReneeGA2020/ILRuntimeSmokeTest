local N = 1000000
local KEY_COUNT = 10000

engine_log("Lua benchmark starting (n=" .. N .. ")")
engine_log("  Lua version: " .. _VERSION)
engine_log("")

-- Init: test engine callback
engine_log("--- Init ---")
engine_log("Plugin alive! calling engine_get_frame...")
local f = engine_get_frame()
engine_log("  got frame = " .. f)

-- Game loop (5 ticks)
engine_log("")
engine_log("--- Game Loop (5 ticks) ---")
for i = 1, 5 do
    local frame = engine_get_frame()
    engine_log("Update tick - frame " .. frame .. ", computing 100+" .. frame .. " = " .. (100 + frame))
end

-- Benchmarks
engine_log("")
engine_log("--- Benchmark ---")

-- 1. Dict (table) lookup
local keys = {}
for i = 1, KEY_COUNT do
    keys[i] = "k" .. i
end

local dict = {}
for i = 1, KEY_COUNT do
    dict[keys[i]] = i
end

local t0 = clock_ms()
local sum = 0
for i = 1, N do
    local idx = ((i - 1) % KEY_COUNT) + 1
    sum = sum + dict[keys[idx]]
end
local t1 = clock_ms()
local dict_ms = t1 - t0
engine_log(string.format("  dict lookup      : %8.2f ms (checksum %d)", dict_ms, sum))

-- 2. Math loop
t0 = clock_ms()
local x = 1.0
for i = 0, N - 1 do
    x = (x * 1.0000001 + i * 0.0000001) % 97.0
end
t1 = clock_ms()
local math_ms = t1 - t0
engine_log(string.format("  math loop        : %8.2f ms (sink %f)", math_ms, x))

-- 3. Method dispatch via metatables (simulating interface/virtual calls)
local Dog = {}
Dog.__index = Dog
function Dog:speak() return 1 end

local Cat = {}
Cat.__index = Cat
function Cat:speak() return 2 end

local dog = setmetatable({}, Dog)
local cat = setmetatable({}, Cat)

t0 = clock_ms()
local v = 0
for i = 1, N do
    if i % 2 == 0 then
        v = v + dog:speak()
    else
        v = v + cat:speak()
    end
end
t1 = clock_ms()
local iface_ms = t1 - t0
engine_log(string.format("  method dispatch  : %8.2f ms (sink %d)", iface_ms, v))

-- 4. Native cross-call (call C function from Lua)
t0 = clock_ms()
local nv = 0
for i = 1, N do
    nv = engine_nop(nv)
end
t1 = clock_ms()
local cross_ms = t1 - t0
local ns_per_call = cross_ms * 1000000 / N
engine_log(string.format("  native cross-call: %8.2f ms (%.0f ns/call, sink %d)", cross_ms, ns_per_call, nv))

-- 5. Property access (table field get/set)
local box = { value = 0 }
t0 = clock_ms()
local pv = 0
for i = 1, N do
    box.value = i
    pv = pv + box.value
end
t1 = clock_ms()
local prop_ms = t1 - t0
engine_log(string.format("  table field g/s  : %8.2f ms (sink %d)", prop_ms, pv))

-- 6. Object allocation
t0 = clock_ms()
local av = 0
for i = 1, N do
    local obj = { value = i }
    av = av + obj.value
end
t1 = clock_ms()
local alloc_ms = t1 - t0
engine_log(string.format("  table alloc+use  : %8.2f ms (sink %d)", alloc_ms, av))

-- 7. Array (table) add + iterate
t0 = clock_ms()
local arr = {}
for i = 1, N do
    arr[i] = i
end
t1 = clock_ms()
local arr_add_ms = t1 - t0

t0 = clock_ms()
local arr_sum = 0
for i = 1, #arr do
    arr_sum = arr_sum + arr[i]
end
t1 = clock_ms()
local arr_iter_ms = t1 - t0
engine_log(string.format("  array add        : %8.2f ms", arr_add_ms))
engine_log(string.format("  array iterate    : %8.2f ms (sum %d)", arr_iter_ms, arr_sum))

-- 8. pcall (equivalent of try/catch)
t0 = clock_ms()
local tv = 0
for i = 1, N do
    local ok, _ = pcall(function() tv = tv + i end)
end
t1 = clock_ms()
local pcall_ms = t1 - t0
engine_log(string.format("  pcall (no error) : %8.2f ms (sink %d)", pcall_ms, tv))

engine_log("")
engine_log("Benchmarks complete.")
