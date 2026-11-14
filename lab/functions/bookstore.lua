#!lua name=bookstore

local function reserve_stock(keys, args)
    local order_id = args[1]
    local quantity = tonumber(args[2])
    if redis.call('HEXISTS', keys[2], order_id) == 1 then
        return -2
    end
    local stock = tonumber(redis.call('GET', keys[1]) or '0')
    if stock < quantity then
        return -1
    end
    redis.call('HSET', keys[2], order_id, quantity)
    return redis.call('DECRBY', keys[1], quantity)
end

redis.register_function('reserve_stock', reserve_stock)
